// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; version 3 of the License.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
// for more details.
//
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Data;
using MariaDB.Data.MySqlClient;

namespace MariaDB.Data.Types
{
    internal struct MySqlUInt64 : IMySqlValue
    {
        private ulong mValue;
        private bool isNull;

        public MySqlUInt64(bool isNull)
        {
            this.isNull = isNull;
            mValue = 0;
        }

        public MySqlUInt64(ulong val)
        {
            this.isNull = false;
            mValue = val;
        }

        public bool IsNull
        {
            get { return isNull; }
        }

        MySqlDbType IMySqlValue.MySqlDbType
        {
            get { return MySqlDbType.UInt64; }
        }

        DbType IMySqlValue.DbType
        {
            get { return DbType.UInt64; }
        }

        object IMySqlValue.Value
        {
            get { return mValue; }
        }

        public ulong Value
        {
            get { return mValue; }
        }

        Type IMySqlValue.SystemType
        {
            get { return typeof(ulong); }
        }

        string IMySqlValue.MySqlTypeName
        {
            get { return "BIGINT"; }
        }

        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            ulong v = (val is ulong) ? (ulong)val : Convert.ToUInt64(val);
            if (binary)
                packet.WriteInteger((long)v, 8);
            else
                packet.WriteStringNoNull(v.ToString());
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            if (nullVal)
                return new MySqlUInt64(true);

            if (length == -1)
                return new MySqlUInt64(packet.ReadULong(8));
            else
                return new MySqlUInt64(UInt64.Parse(packet.ReadString(length)));
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.Position += 8;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            // we use name indexing because this method will only be called
            // when GetSchema is called for the DataSourceInformation
            // collection and then it will be cached.
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "BIGINT";
            row["ProviderDbType"] = MySqlDbType.UInt64;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "BIGINT UNSIGNED";
            row["CreateParameters"] = null;
            row["DataType"] = "System.UInt64";
            row["IsAutoincrementable"] = true;
            row["IsBestMatch"] = true;
            row["IsCaseSensitive"] = false;
            row["IsFixedLength"] = true;
            row["IsFixedPrecisionScale"] = true;
            row["IsLong"] = false;
            row["IsNullable"] = true;
            row["IsSearchable"] = true;
            row["IsSearchableWithLike"] = false;
            row["IsUnsigned"] = true;
            row["MaximumScale"] = 0;
            row["MinimumScale"] = 0;
            row["IsConcurrencyType"] = DBNull.Value;
            row["IsLiteralSupported"] = false;
            row["LiteralPrefix"] = null;
            row["LiteralSuffix"] = null;
            row["NativeDataType"] = null;
            dsTable.Rows.Add(row);
        }
    }
}