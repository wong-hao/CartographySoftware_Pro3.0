using ArcGIS.Core.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMGI_Common
{
    public class Helper
    {
        /// <summary>
        /// 读取mdb数据库表
        /// </summary>
        /// <param name="mdbFilePath"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static DataTable ReadToDataTable(string mdbFilePath, string tableName)
        {
            DataTable pDataTable = new DataTable();
            try
            {
                using (var connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=" + mdbFilePath))
                {
                    connection.Open();
                    string sql = "select * from " + tableName;
                    //获取目标表的内容
                    OleDbDataAdapter dbDataAdapter = new OleDbDataAdapter(sql, connection); //创建适配对象
                    DataTable pTable = new DataTable(); //新建表对象
                    dbDataAdapter.Fill(pTable); //用适配对象填充表对象
                                                //新表添加表头。
                    DataColumn[] rows = pTable.Columns.Cast<DataColumn>().ToArray();
                    for (int i = 0; i < rows.Length; i++)
                    {
                        DataColumn newDc = new DataColumn() { ColumnName = rows[i].ColumnName };
                        pDataTable.Columns.Add(newDc);
                    }
                    //为新表添加内容。
                    foreach (var oneRow in pTable.Rows)
                    {
                        DataRow dataRow = oneRow as DataRow;
                        DataRow dr = pDataTable.NewRow();
                        for (int i = 0; i < dataRow.ItemArray.Length; i++)
                        {
                            object obValue = dataRow.ItemArray[i];
                            if (obValue != null && !Convert.IsDBNull(obValue))
                            {
                                dr[i] = obValue;
                            }
                            else
                            {
                                dr[i] = "";
                            }
                        }
                        pDataTable.Rows.Add(dr);
                    }
                }
                return pDataTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return pDataTable;
            }
        }

        /// <summary>
        /// 获取所有表名
        /// </summary>
        /// <param name="mdbFilePath"></param>
        /// <returns></returns>
        public static List<string> GetAllTableNames(string mdbFilePath)
        {
            List<string> tableNames = new List<string>();
            try
            {
                using (var connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=" + mdbFilePath))
                {
                    connection.Open();
                    var dt = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        tableNames.Add(dt.Rows[i].ItemArray[2].ToString());
                    }
                    connection.Close();
                }
                return tableNames;
            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.Message);
                return tableNames;
            }
        }

        /// <summary>
        /// 读取gdb数据库
        /// </summary>
        /// <param name="gdbFilePath"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static DataTable ReadGDBToDataTable(string gdbFilePath, string tableName)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(gdbFilePath))))
                using (Table table = geodatabase.OpenDataset<Table>(tableName))
                {
                    foreach (Field field in table.GetDefinition().GetFields())
                    {
                        // 使用字段名作为列名
                        string columnName = field.Name;

                        // 检查列是否已经存在于DataTable中
                        if (!dataTable.Columns.Contains(columnName))
                        {
                            // 如果列不存在，则将其添加到DataTable中
                            dataTable.Columns.Add(columnName, typeof(string));
                        }
                    }

                    using (RowCursor rowCursor = table.Search(null, false))
                    {
                        while (rowCursor.MoveNext())
                        {
                            using (Row row = rowCursor.Current)
                            {
                                DataRow dataRow = dataTable.NewRow();
                                foreach (Field field in table.GetDefinition().GetFields())
                                {
                                    object value = row[field.Name];
                                    dataRow[field.Name] = value != null ? value.ToString() : string.Empty;
                                }
                                dataTable.Rows.Add(dataRow);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return dataTable;
        }
    }
}
