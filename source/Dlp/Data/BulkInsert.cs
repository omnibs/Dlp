/* 
 * Written by Ronnie Overby
 * and part of the Ronnie Overby Grab Bag: https://github.com/ronnieoverby/RonnieOverbyGrabBag
 */

 // http://nuget.org/packages/fasterflect -- PM> Install-Package fasterflect

namespace Dlp.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;

    using Fasterflect;

    public class BulkInsertEventArgs<T> : EventArgs
    {
        public BulkInsertEventArgs(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            this.Items = items.ToArray();
        }

        public T[] Items { get; private set; }
    }

    /// <summary>
    /// Performs buffered bulk inserts into a sql server table using objects instead of DataRows. :)
    /// </summary>
    public class BulkInserter<T> where T : class
    {
        public event EventHandler<BulkInsertEventArgs<T>> PreBulkInsert;
        public void OnPreBulkInsert(BulkInsertEventArgs<T> e)
        {
            var handler = this.PreBulkInsert;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<BulkInsertEventArgs<T>> PostBulkInsert;
        public void OnPostBulkInsert(BulkInsertEventArgs<T> e)
        {
            var handler = this.PostBulkInsert;
            if (handler != null) handler(this, e);
        }

        private const int DefaultBufferSize = 2000;
        private readonly SqlConnection _connection;
        private readonly int _bufferSize;
        public int BufferSize { get { return this._bufferSize; } }

        private readonly Lazy<Dictionary<string, MemberGetter>> _props =
            new Lazy<Dictionary<string, MemberGetter>>(GetPropertyInformation);

        private readonly Lazy<DataTable> _dt;

        private readonly SqlBulkCopy _sbc;
        private readonly List<T> _queue = new List<T>();

        /// <param name="connection">SqlConnection to use for retrieving the schema of sqlBulkCopy.DestinationTableName</param>
        /// <param name="sqlBulkCopy">SqlBulkCopy to use for bulk insert.</param>
        /// <param name="bufferSize">Number of rows to bulk insert at a time. The default is 5000.</param>
        public BulkInserter(SqlConnection connection, SqlBulkCopy sqlBulkCopy, int bufferSize = DefaultBufferSize)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (sqlBulkCopy == null) throw new ArgumentNullException("sqlBulkCopy");

            this._bufferSize = bufferSize;
            this._connection = connection;
            this._sbc = sqlBulkCopy;
            this._dt = new Lazy<DataTable>(this.CreateDataTable);
        }

        /// <param name="connection">SqlConnection to use for retrieving the schema of sqlBulkCopy.DestinationTableName and for bulk insert.</param>
        /// <param name="tableName">The name of the table that rows will be inserted into.</param>
        /// <param name="bufferSize">Number of rows to bulk insert at a time. The default is 5000.</param>
        /// <param name="copyOptions">Options for SqlBulkCopy.</param>
        /// <param name="sqlTransaction">SqlTransaction for SqlBulkCopy</param>
        public BulkInserter(SqlConnection connection, string tableName, int bufferSize = DefaultBufferSize,
                            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default, SqlTransaction sqlTransaction = null)
            : this(connection, new SqlBulkCopy(connection, copyOptions, sqlTransaction) { DestinationTableName = tableName }, bufferSize)
        {
        }

        /// <summary>
        /// Performs buffered bulk insert of enumerable items.
        /// </summary>
        /// <param name="items">The items to be inserted.</param>
        public void Insert(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");

            // get columns that have a matching property
            var cols = this._dt.Value.Columns.Cast<DataColumn>()
                .Where(x => this._props.Value.ContainsKey(x.ColumnName))
                .Select(x => new { Column = x, Getter = this._props.Value[x.ColumnName] })
                .Where(x => x.Getter != null)
                .ToArray();

            foreach (var buffer in this.Buffer(items))
            {
                foreach (var item in buffer)
                {
                    var row = this._dt.Value.NewRow();

                    foreach (var col in cols)
                        row[col.Column] = col.Getter(item);

                    this._dt.Value.Rows.Add(row);
                }

                var bulkInsertEventArgs = new BulkInsertEventArgs<T>(buffer);
                this.OnPreBulkInsert(bulkInsertEventArgs);

                this._sbc.WriteToServer(this._dt.Value);

                this.OnPostBulkInsert(bulkInsertEventArgs);

                this._dt.Value.Clear();
            }
        }

        /// <summary>
        /// Queues a single item for bulk insert. When the queue count reaches the buffer size, bulk insert will happen.
        /// Call Flush() to manually bulk insert the currently queued items.
        /// </summary>
        /// <param name="item">The item to be inserted.</param>
        public void Insert(T item)
        {
            if (item == null) throw new ArgumentNullException("item");

            this._queue.Add(item);

            if (this._queue.Count == this._bufferSize)
                this.Flush();
        }

        /// <summary>
        /// Bulk inserts the currently queued items.
        /// </summary>
        public void Flush()
        {
            this.Insert(this._queue);
            this._queue.Clear();
        }

        private static Dictionary<string, MemberGetter> GetPropertyInformation()
        {
            return typeof(T).Properties().ToDictionary(x => x.Name, x => x.DelegateForGetPropertyValue());
        }

        private DataTable CreateDataTable()
        {
            var dt = new DataTable();
            using (var cmd = this._connection.CreateCommand())
            {
                cmd.CommandText = string.Format("select top 0 * from {0}", this._sbc.DestinationTableName);

                using (var reader = cmd.ExecuteReader())
                    dt.Load(reader);
            }

            return dt;
        }

        private IEnumerable<T[]> Buffer(IEnumerable<T> enumerable)
        {
            var buffer = new List<T>();
            foreach (var item in enumerable)
            {
                buffer.Add(item);
                if (buffer.Count >= this.BufferSize)
                {
                    yield return buffer.ToArray();
                    buffer.Clear();
                }
            }

            if (buffer.Count > 0)
                yield return buffer.ToArray();
        }
    }
}