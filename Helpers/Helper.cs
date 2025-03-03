using System.Data;
using Microsoft.Data.SqlClient;

public class Helper
{
    public DataSet FDataSet(SqlCommand command)
    {
        using (var adapter = new SqlDataAdapter(command))
        {
            var ds = new DataSet();
            adapter.Fill(ds);
            return ds;
        }
    }
}