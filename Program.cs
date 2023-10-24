using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {

        //connection string. Add UID=username; PWD=password; to the connection string if not a public db
        string connectionString = "SERVER=DESKTOP-Q9CEJ1Q\\SQLExpress; DATABASE=demo_db; Trusted_Connection=true;";

        //API URL
        string apiUrl = "http://localhost:7001/endpoint";

        int pollingIntervalInSeconds = 60;

        while (true)
        {
            try
            {
                // Read a value from the SQL table
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    //mock random query
                    Random random = new Random();
                    int randomNumber = random.Next(1, 4);

                    //query
                    string sqlQuery = "SELECT name,age FROM students where id = "+randomNumber.ToString();

                    //reading data from the MSSQLExpress db
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {

                            //add the data wanted into variable here
                            string name = reader.GetString(reader.GetOrdinal("name"));
                            int age = reader.GetInt32(reader.GetOrdinal("age"));

                            // Push the value to the remote API
                            using (HttpClient httpClient = new HttpClient())
                            {
                                //sample object object passed in the body
                                //set the variables to the object here
                                var dataObject = new
                                {
                                    name = name,
                                    age = age
                                };

                                string json = System.Text.Json.JsonSerializer.Serialize(dataObject);

                                var content = new StringContent(json, Encoding.UTF8, "application/json");
                                var response = await httpClient.PostAsync(apiUrl, content);

                                if (response.IsSuccessStatusCode)
                                {
                                    string responseContent = await response.Content.ReadAsStringAsync();
                                    Console.WriteLine($"Success: {responseContent}\n");
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to push value to the API. Status Code: {response.StatusCode}\n");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No data to push from the SQL table.\n");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error while reading from SQL: {ex.Message}\n");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error while pushing data to the API: {ex.Message}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}\n");
            }

            // Wait for the specified interval before polling again
            Thread.Sleep(pollingIntervalInSeconds * 1000);
        }
    }
}
