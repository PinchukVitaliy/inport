using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Data.SqlClient;
using System.Linq;
using System.Data.Entity;
using System.IO;
using System.ComponentModel;


namespace importusers
{
    public partial class Form1 : Form
    {
        private const string APP_PATH = "http://localhost:5000";
        string APP_PATH_NEW = string.Empty;
        //private ChatBotAppDataBaseTestEntities db;

        const string stateid_on = "state_03";
        const string stateid_off = "state_02";

        DateTime start;
        DateTime end;

        DataTable dt = new DataTable();
        public Form1()
        {
            InitializeComponent();

            //ServicePointManager.Expect100Continue = true;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //отключить сертификат к api
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            label1.Text = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "*.xls;*.xlsx";
            ofd.Filter = "Microsoft Excel (*.xls*)|*.xls*";
            ofd.Title = "Выберите документ для загрузки данных";
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("Вы не выбрали файл для открытия", "Загрузка данных...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                button1.Enabled = false;
                button2.Enabled = false;
                label1.Text = "Идет выгрузка с файла на форму... Это займет некоторое время!";
                this.Refresh();

                String constr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                                    ofd.FileName + ";Extended Properties='Excel 12.0 XML;HDR=YES;IMEX=1';";

                System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(constr);
                con.Open();
                DataSet ds = new DataSet();
                DataTable schemaTable = con.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                string sheet1 = (string)schemaTable.Rows[0].ItemArray[2];
                string select = String.Format("SELECT * FROM [{0}]", sheet1);
                System.Data.OleDb.OleDbDataAdapter ad = new System.Data.OleDb.OleDbDataAdapter(select, con);
                ad.Fill(ds);
                dt = ds.Tables[0];
                con.Close();
                con.Dispose();
                dataGridView1.DataSource = dt;

                button1.Enabled = true;
                button2.Enabled = dt.Rows.Count > 0 ? true : false;
                label1.Text = "Спасибо за ожидание)";
            }
        }

        private string GetUserInfo(string code)
        {
            APP_PATH_NEW = checkBox1.Checked ? textBox1.Text.Trim() : APP_PATH;

            string json = "{\"custAccountId\":\"" + code + "\"}";

            StringContent sc = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClient c = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });

            var x = c.PostAsync(APP_PATH_NEW + "/Meter/find", sc).Result; // returns 200

            if (x.StatusCode == HttpStatusCode.OK)
                return x.Content.ReadAsStringAsync().Result;
            else
                return string.Empty;
        }
     
        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                start = DateTime.Now;
                button2.Enabled = false;
                progressBarLoad.Value = 0;             
                progressBarLoad.Minimum = 0;
                progressBarLoad.Maximum = dt.Rows.Count;
                progressBarLoad.Step = 1;

                var db = new ChatBotAppDataBaseTestEntities();

                //var dir = Directory.GetCurrentDirectory() + "\\error_rows\\ErrorList.csv";
                var path = @"C:\Users\Пинчук Виталий\source\repos\importusers\error_rows\ErrorList.csv";
                using (var streamWriter = new StreamWriter(path, false, Encoding.GetEncoding(1251)))
                {
                    var columns = dt.Columns.Cast<DataColumn>();
                    streamWriter.WriteLine(string.Join(";", columns));

                    foreach (DataRow row in dt.Rows)
                    {
                        string number = row["personal_account_number"].ToString();
                        string name = row["full_name"].ToString();
                        var telegram = row["telegram_id"];
                        var viber = row["viber_id"];
                        string telephon = row["phone"].ToString();

                        string correctJSON = GetUserInfo(number);

                        if (!correctJSON.Equals(string.Empty))
                        {
                            Client resultClient = new Client(correctJSON);
                            Thread.Sleep(1);

                            CustTable newCustTable = await db.CustTable.FirstOrDefaultAsync(
                                f => f.TelephoneNumber == "+" + telephon.Trim());
                            if (newCustTable == null)
                            {
                                string CustId = await GenerateIdToProcAsync("CustTable", db);
                                newCustTable = InitCustTable(CustId, telephon, "1407");
                                db.CustTable.Add(newCustTable);
                            }

                            CustAccounts newCustAccounts = await db.CustAccounts
                                .FirstOrDefaultAsync(f => f.KontAccountId == resultClient.kontAccountId.Trim() 
                                && f.CompanyId == newCustTable.CompanyId.Trim()
                                && f.CustId == newCustTable.CustId.Trim());
                            if (newCustAccounts == null)
                            {
                                newCustAccounts = InitCustAccounts(resultClient, newCustTable);
                                db.CustAccounts.Add(newCustAccounts);
                            }

                            EngChatTable newEngChatTableTel = await InitEngChatTable(db, telegram.ToString(), name, "Telegram", newCustTable, stateid_on);
                            if (newEngChatTableTel != null)
                                db.EngChatTable.Add(newEngChatTableTel);

                            EngChatTable newEngChatTableVib = await InitEngChatTable(db, viber.ToString(), name, "Viber", newCustTable, stateid_on);
                            if (newEngChatTableVib != null)
                                db.EngChatTable.Add(newEngChatTableVib);

                            CustMeterPoints newCustMeterPoints = await db.CustMeterPoints.FirstOrDefaultAsync(
                                d => d.EICCode == resultClient.eicCode.Trim());
                            if (newCustMeterPoints == null)
                            {
                                newCustMeterPoints = InitCustMeterPoints(resultClient, newCustTable);
                                db.CustMeterPoints.Add(newCustMeterPoints);
                            }

                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            streamWriter.WriteLine(string.Join(";", row.ItemArray));

                            CustTable newCustTable = await db.CustTable.FirstOrDefaultAsync(
                                f => f.TelephoneNumber == "+" + telephon.Trim());
                            if (newCustTable == null)
                            {
                                string CustId = await GenerateIdToProcAsync("CustTable", db);
                                newCustTable = InitCustTable(CustId, telephon, "1407");
                                db.CustTable.Add(newCustTable);
                            }
                            EngChatTable newEngChatTableTel = await InitEngChatTable(db, telegram.ToString(), name, "Telegram", newCustTable, stateid_off);
                            if (newEngChatTableTel != null)
                                db.EngChatTable.Add(newEngChatTableTel);

                            EngChatTable newEngChatTableVib = await InitEngChatTable(db, viber.ToString(), name, "Viber", newCustTable, stateid_off);
                            if (newEngChatTableVib != null)
                                db.EngChatTable.Add(newEngChatTableVib);

                            await db.SaveChangesAsync();
                        }
                        progressBarLoad.PerformStep();
                    }
                }
                end = DateTime.Now;
                MessageBox.Show(start.ToString() + " --- " + end.ToString());
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.InnerException);
            }
        }
        private async Task<string> GenerateIdToProcAsync(string nameTab, ChatBotAppDataBaseTestEntities context)
        {
            int? quantity = 0;
            var ParamTable = new SqlParameter("ParamTable", nameTab);
            var maskLabel = new SqlParameter("genmask", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output };
            try
            {
                quantity = await context.Database.ExecuteSqlCommandAsync("exec dbo.GetNumberSequenceReference @ParamTable, @genmask output",
                    new[] { ParamTable, maskLabel });
                return (string)maskLabel.Value;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private CustTable InitCustTable(string custid, string telephon, string company)
        {
            return new CustTable()
            {
                CustId = custid,
                CompanyId = company,
                TelephoneNumber = "+" + telephon,
                CreateDateTime = DateTime.Now
            };
        }
        private CustAccounts InitCustAccounts(Client resultClient, CustTable newCustTable)
        {
            return new CustAccounts()
            {
                KontAccountId = resultClient.kontAccountId,
                CompanyId = newCustTable.CompanyId,
                CustAccountId = resultClient.custAccountId,
                WorksId = resultClient.worksId,
                WorksName = resultClient.worksName,
                CustType = resultClient.custType.ToString(),
                GreenTariff = resultClient.greenTariff.ToString(),
                CustId = newCustTable.CustId,
                CreateDateTime = DateTime.Now
            };
        }

        private async Task<EngChatTable> InitEngChatTable(
            ChatBotAppDataBaseTestEntities db,
            string vibertelegram,
            string name,
            string type,
            CustTable newCustTable,
            string stateid)
        {
            if (vibertelegram.ToString() != "NULL")
            {
                var id = await db.EngChatTable.FirstOrDefaultAsync(d => d.UserDialogId == vibertelegram.ToString());
             
                if (id == null)
                {
                    return new EngChatTable()
                    {
                        UserDialogId = vibertelegram.ToString(),
                        Login = name.ToString(),
                        TextMessage = "",
                        StateId = stateid,
                        TempStateId = null,
                        LanguageId = "ua",
                        CompanyId = newCustTable.CompanyId,
                        CustId = newCustTable.CustId,
                        MessengerType = type,
                        ChatId = await GenerateIdToProcAsync("EngChatTable", db),
                        CreateDateTime = DateTime.Now,
                        ModifyDateTime = DateTime.Now
                    };
                }

                if (stateid == stateid_on && id != null && id.StateId == stateid_off)
                {
                    id.StateId = stateid_on;
                }
            }
            return null;
        }

        private CustMeterPoints InitCustMeterPoints(Client resultClient, CustTable newCustTable)
        {
            return new CustMeterPoints()
            {
                KontAccountId = resultClient.kontAccountId,
                City = resultClient.city.ToString(),
                District = resultClient.district,
                Street = resultClient.street,
                HouseNumber = resultClient.houseNumber,
                PostCode = resultClient.postCode,
                Corp = resultClient.corpsNumber,
                FlatNumber = resultClient.flatNumber,
                EICCode = resultClient.eicCode,
                SerialNumber = resultClient.serialNumber,
                CompanyId = newCustTable.CompanyId,
                CreateDateTime = DateTime.Now
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Enabled = checkBox1.Checked ? true : false;
            textBox1.Text = APP_PATH;          
            label1.Text = "";
            button2.Enabled = false;  
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var dbb = new ChatBotAppDataBaseTestEntities())
            {                
                try
                {
                    dbb.Database.Connection.Open();
                    dbb.Database.Connection.Close();

                    MessageBox.Show("Все в порядке");

                    button2.Enabled = true;
                }
                catch (SqlException sql)
                {
                    button2.Enabled = false;
                    MessageBox.Show(sql.Message);
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox1.Enabled = true;
            }
            else
            {
                textBox1.Text = APP_PATH;
                textBox1.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                var test = checkBox1.Checked ? textBox1.Text.Trim() : APP_PATH;
                //string json = "{\"custAccountId\":\"\"}";

                //StringContent sc = new StringContent(json, Encoding.UTF8, "application/json");

                HttpClient c = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });

                //var x = c.PostAsync(test + "/Meter/find", sc).Result;
                var w = c.GetAsync(test+"/test").Result;

                MessageBox.Show(w.StatusCode.ToString());

                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                button2.Enabled = false;
                MessageBox.Show(ex.Message + "\n" + ex.InnerException);
            }
        }
    }
}
