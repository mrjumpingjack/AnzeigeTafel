using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Linq.Expressions;
using AnzeigeTafel;
using System.Runtime.Remoting.Messaging;

namespace AnzeigeTafel
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        bool items_saved = false;
        string speichern_unter_pfad;

        private int SERVERPORT;
        private string SERVERADDRESS;



        private string LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        }



        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (items_saved)
            {
                this.Close();
            }
            else
            {
                DialogResult m_result = MessageBox.Show(this, "Die aktuell angezeigten Meldungen sind nicht gespeichert!" + Environment.NewLine + "Möchten Sie diese jetzt speichern?", "Warnung", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);


                if (m_result == DialogResult.Yes)
                {
                    speichern_unter();
                }
                else if (m_result == DialogResult.No)
                {
                    this.Close();

                }
                else if (m_result == DialogResult.Cancel)
                {

                }

            }


        }

        private void neuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (items_saved)
            {
                Senden("$%INFO:_del_at,-1");

                dataGridView1.Rows.Clear();
                speichern_unter_pfad = null;
            }
            else
            {
                DialogResult m_result = MessageBox.Show(this, "Die aktuell angezeigten Meldungen sind nicht gespeichert!" + Environment.NewLine + "Möchten Sie diese jetzt speichern?", "Warnung", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);


                if (m_result == DialogResult.Yes)
                {
                    speichern_unter();
                    items_saved = true;
                    Senden("$%INFO:_del_at,-1");
                    dataGridView1.Rows.Clear();
                    speichern_unter_pfad = null;
                }
                else if (m_result == DialogResult.No)
                {
                    items_saved = true;
                    Senden("$%INFO:_del_at,-1");
                    dataGridView1.Rows.Clear();
                    speichern_unter_pfad = null;
                }
                else if (m_result == DialogResult.Cancel)
                {

                }

            }
        }

        private void speichernToolStripMenuItem_Click(object sender, EventArgs e)
        {
            speichern_unter();
        }

        private void speichernUnterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            speichern_unter();
        }

        private void ladenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (items_saved)
            {
               
                laden();

            }
            else
            {
                DialogResult m_result = MessageBox.Show(this, "Die aktuell angezeigten Meldungen sind nicht gespeichert!" + Environment.NewLine + "Möchten Sie diese jetzt speichern?", "Warnung", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);


                if (m_result == DialogResult.Yes)
                {
                    speichern_unter();
                    laden();



                }
                else if (m_result == DialogResult.No)
                {
                    laden();

                }
                else if (m_result == DialogResult.Cancel)
                {

                }
            }
        }

        private void speichern_unter()
        {
            try
            {
                if (String.IsNullOrEmpty(speichern_unter_pfad))
                {

                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName))
                        {
                            foreach (DataGridViewRow row in dataGridView1.Rows)
                            {
                                sw.WriteLine(row.Cells[0].Value.ToString() + ";$%OVERHEAD$%:" + row.Cells[0].Style.BackColor.ToArgb());
                            }
                            sw.Close();
                        }

                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName))
                    {
                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            sw.WriteLine(row.Cells[0].Value.ToString() + ";$%OVERHEAD$%:" + row.Cells[0].Style.BackColor.ToArgb());
                        }
                        sw.Close();
                    }

                }

                speichern_unter_pfad = saveFileDialog1.FileName;
                items_saved = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beim Speichern der Datei ist ein Fehler aufgetreten.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void laden()
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    items_saved = true;
                    Senden("$%INFO:_del_at,-1");
                    dataGridView1.Rows.Clear();

                    using (StreamReader sr = new StreamReader(openFileDialog1.FileName))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string[] data = sr.ReadLine().Split(new string[] { ";$%OVERHEAD$%:" }, StringSplitOptions.None);

                            string text = data[0];
                            string color = data[1];

                            Senden_color(text, Convert.ToInt32(color));
                            add_data_to_dgv(text, Color.Black, Color.FromArgb(Convert.ToInt32(color)));
                        }
                        sr.Close();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Beim Laden der Datei ist ein Fehler aufgetreten.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        public void Senden_color(string data, int color)
        {


            data = data + ";$%OVERHEAD$%:" + color + Environment.NewLine;

            foreach (HandleClientRequest h in hcr)
            {
                h.Send(data);
            }

        }

        public void Senden(string data)
        {
            foreach (HandleClientRequest h in hcr)
            {
                h.Send(data + Environment.NewLine);
            }

        }



        private void IsNewData()
        {
            try
            {
                String old_data = "";

                while (true)
                {
                    if (datacenter.New_Data != null)
                    {
                        String[] data_ = datacenter.New_Data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        datacenter.New_Data = null;

                        foreach (string data in data_)
                        {
                            if (data != null && data.Length > 0 && data != old_data)
                            {
                                if (!data.StartsWith("$%INFO:"))
                                {
                                    items_saved = false;
                                    add_data_to_dgv(data.Split(new string[] { ";$%OVERHEAD$%:" }, StringSplitOptions.None)[0], Color.Black,
                                        Color.FromArgb(Convert.ToInt32(data.Split(new string[] { ";$%OVERHEAD$%:" }, StringSplitOptions.None)[1])));

                                    Senden(data);
                                }
                                else
                                {
                                    if (data.StartsWith("$%INFO:_del_at"))
                                    {
                                        if (data.StartsWith("$%INFO:_del_at,-1"))
                                        {
                                            this.Invoke((MethodInvoker)delegate
                                            {

                                                dataGridView1.Rows.Clear();

                                            });
                                        }
                                        else
                                        {

                                            this.Invoke((MethodInvoker)delegate
                                            {
                                                dataGridView1.Rows.RemoveAt(Convert.ToInt32(data.Split(new char[] { ',' })[1]));
                                            });

                                        }

                                        Senden(data);
                                    }
                                    else
                                    {
                                        //Senden("$%INFO:_edit_at,
                                        //" + edit_index.ToString() + "," +
                                        //dataGridView1.CurrentCell.Value.ToString() + ";$%OVERHEAD$%:" +
                                        // panel1.BackColor.ToArgb());

                                        if (data.StartsWith("$%INFO:_edit_at"))
                                        {

                                            string[] infos = data.Split(new string[] { ",", ";$%OVERHEAD$%:" }, StringSplitOptions.RemoveEmptyEntries);

                                            int row_index = Convert.ToInt32(infos[1]);
                                            string new_text = infos[2];
                                            Color new_color = Color.FromArgb(Convert.ToInt32(infos[3]));
                                            this.Invoke((MethodInvoker)delegate
                                            {
                                                dataGridView1.Rows[row_index].Cells[0].Value = new_text;
                                                dataGridView1.Rows[row_index].Cells[0].Style.BackColor = new_color;
                                            });

                                            Senden("$%INFO:_edit_at," + row_index.ToString() + "," + new_text + ";$%OVERHEAD$%:" + new_color.ToArgb() + Environment.NewLine);

                                        }


                                    }
                                }

                                old_data = data;
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {

            }
        }


        public int fontgrösse=8;

        private void einstellungenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings settings = new Settings(server_running, SERVERADDRESS, SERVERPORT,fontgrösse);
            settings.ShowDialog();
            SERVERADDRESS = settings.IP;
            SERVERPORT = settings.PORT;
            fontgrösse = settings.FONTGRÖSSE;

           this.dataGridView1.DefaultCellStyle.Font = new Font(dataGridView1.Font.FontFamily, (float)Convert.ToDecimal(fontgrösse));

         

            //listView1.Font = new Font(listView1.Font.FontFamily,(float)Convert.ToDecimal(fontgrösse));
            //listView1.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);


        }
        public bool server_running = false;

        Thread checkfornewdata;
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartServer();

            checkfornewdata = new Thread(IsNewData);
            checkfornewdata.Start();

            add_data_to_dgv("Der Sever ist erreichbar unter: " + SERVERADDRESS + ":" + SERVERPORT,Color.Black,Color.LightGreen);

          //  einstellungenToolStripMenuItem.Enabled = false;

        }

        private void get_settings()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel/settings.conf"))
                {
                    string[] settings = sr.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    String IP = settings[0].Split(new char[] { ':' })[1];
                    String PORT = settings[1].Split(new char[] { ':' })[1];
                    sr.Close();

                    SERVERADDRESS = IP;
                    SERVERPORT = Convert.ToInt32(PORT);
                }

            }
            catch (DirectoryNotFoundException ex)
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel");
                get_settings();
            }
            catch (FileNotFoundException ex)
            {
             //   File.Create(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel/settings.conf");

                using (StreamWriter sw = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel/settings.conf", false))
                {
                    sw.WriteLine("IPADDRESS:" + "192.168.111.66");
                    sw.WriteLine("PORT:" + "56564");
                    sw.Close();
                }

                SERVERADDRESS = "192.168.111.66";
                SERVERPORT = Convert.ToInt32("56564");

                MessageBox.Show("Einstellungen konnten nicht geladen werden. Die Standarteinstellungen werden geladen", "Einstellungen konnten nicht geladen werden.", MessageBoxButtons.OK);
            }


        }



     



        private void Form1_Load(object sender, EventArgs e)
        {
            SERVERADDRESS = LocalIPAddress();
           
            dataGridView1.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";

            get_settings();
        }



        private static TcpListener _listener;

        public void StartServer()
        {
            try
            {


                System.Net.IPAddress localIPAddress = System.Net.IPAddress.Parse(SERVERADDRESS);
                IPEndPoint ipLocal = new IPEndPoint(localIPAddress, SERVERPORT);
                _listener = new TcpListener(ipLocal);
                _listener.Start();
                WaitForClientConnect();
                server_running = true;

                startToolStripMenuItem.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beim Starten des Servers ist ein Fehler aufgetreten.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WaitForClientConnect()
        {
            try
            {
                object obj = new object();
                _listener.BeginAcceptTcpClient(new System.AsyncCallback(OnClientConnect), obj);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        static List<HandleClientRequest> hcr = new List<HandleClientRequest>();

        TcpClient clientSocket = default(TcpClient);

        private void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                clientSocket = _listener.EndAcceptTcpClient(asyn);
                HandleClientRequest clientReq = new HandleClientRequest(clientSocket);
                clientReq.StartClient();
                hcr.Add(clientReq);


                this.Invoke((MethodInvoker)delegate
                {

                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {

                        clientReq.Send(dataGridView1.Rows[i].Cells[0].Value.ToString() + ";$%OVERHEAD$%:" + dataGridView1.Rows[i].Cells[0].Style.BackColor.ToArgb() + Environment.NewLine);

                        Thread.Sleep(10);
                    }
                });
            }
            catch (Exception se)
            {
                throw;
            }

            WaitForClientConnect();
        }


        private void add_data_to_dgv(string data,Color forecolor,Color backcolor)
        {
            if (!string.IsNullOrEmpty(data))
            {
                this.Invoke((MethodInvoker)delegate
                {
                    dataGridView1.Rows.Add(data);

                    dataGridView1.Rows[dataGridView1.Rows.Count-1].Cells[0].Style.BackColor = backcolor;

                    dataGridView1.CurrentCell =null;
                });

            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (checkfornewdata != null && checkfornewdata.ThreadState == ThreadState.Running)
            {
                checkfornewdata.Abort();
            }
            foreach (HandleClientRequest h in hcr)
            {
                h.stopp();
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.CurrentCell = null;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            dataGridView1.CurrentCell = null;
        }
    }


    public class HandleClientRequest
    {


        TcpClient _clientSocket;
        NetworkStream _networkStream = null;


        public HandleClientRequest(TcpClient clientConnected)
        {
            this._clientSocket = clientConnected;
        }
        public void StartClient()
        {
            _networkStream = _clientSocket.GetStream();
            WaitForRequest();
        }

        public void WaitForRequest()
        {
            byte[] buffer = new byte[_clientSocket.ReceiveBufferSize];

            _networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, buffer);
        }


        public void stopp()
        {
            _clientSocket.Close();
            networkStream.Close();
        }




        public void Send(String data)
        {
            NetworkStream networkStream = _clientSocket.GetStream();
            //do the job with the data here
            //send the data back to client.
            Byte[] sendBytes = Encoding.UTF8.GetBytes(data+Environment.NewLine);
            networkStream.Write(sendBytes, 0, sendBytes.Length);
            networkStream.Flush();
        }


        NetworkStream networkStream;

        public void ReadCallback(IAsyncResult result)
        {
            try
            {
                networkStream = _clientSocket.GetStream();

                int read = networkStream.EndRead(result);
                if (read == 0)
                {
                    _networkStream.Close();
                    _clientSocket.Close();
                    return;
                }

                byte[] buffer = result.AsyncState as byte[];
                string data = Encoding.UTF8.GetString(buffer, 0, read);

               


                Console.WriteLine(data.Split(new string[] { ";$%OVERHEAD$%:" }, StringSplitOptions.None)[0]);
                datacenter.New_Data = data;
            }
            catch (Exception ex)
            {
                throw;
            }
            this.WaitForRequest();
        }
    }





    public static class datacenter
    {
        static private string new_data;
        static public string New_Data
        {
            get
            {

                return new_data;
            }

            set
            {
                new_data = value;
            }

        }

        static private List<String> all_msg;
        static public List<String> All_MSG
        {
            get
            {
                return all_msg;
            }

            set
            {
                all_msg = value;
            }

        }
    }







  


}