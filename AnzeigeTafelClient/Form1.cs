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

namespace AnzeigeTafelClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Thread recive;

        bool is_started = false;

        private void Form1_Load(object sender, EventArgs e)
        {
           // SERVERADD = LocalIPAddress();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";

            dataGridView1.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            get_settings();

            splitContainer1.Panel2MinSize = 50;

            if (!is_started)
            {
                button1.Text = "Verbinden";
            }


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
               // File.Create(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel/settings.conf");

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
        

        public String SERVERADDRESS;

        public String MSG;

        public Int32 SERVERPORT;

        public NetworkStream stream;

        public int CLIENT_ID = -1;

        public string MY_ADDRESS;


        TcpClient client;


        private string LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        }


        public bool Connect()
        {
            // Create a TcpClient.
            // Note, for this client to work you need to have a TcpServer 
            // connected to the same address as specified by the server, port
            // combination.

            try
            {
                client = new TcpClient(SERVERADDRESS, SERVERPORT);
                client.SendTimeout = 2000;
                //client.Connect(SERVERADD, SERVERPORT);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            return true;
        }


        public void listen()
        {

            MSG = "$%INFO:" + client.Client.LocalEndPoint.Serialize().ToString();
            MY_ADDRESS = ((IPEndPoint)client.Client.LocalEndPoint).Address.ToString() + ":" + ((IPEndPoint)client.Client.LocalEndPoint).Port.ToString();
            this.Invoke((MethodInvoker)delegate
            {
                this.Text = MY_ADDRESS;
            });


            Senden(MSG);

            Byte[] data = new Byte[4096];

            while (true)
            {
                try
                {

                    // String to store the response ASCII representation.
                    String resdata = String.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    resdata = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

                    string[] responseDatas = resdata.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);


                    foreach (string responseData in responseDatas)
                    {
                        if (responseData != "")
                        {
                            Console.WriteLine("Received: {0}", responseData);


                            if (!responseData.StartsWith("$%INFO:"))
                            {
                                string text = responseData.Split(new string[] { ";$%OVERHEAD$%:" }, StringSplitOptions.None)[0];
                                string color = responseData.Split(new string[] { ";$%OVERHEAD$%:" }, StringSplitOptions.None)[1];

                                items_saved = false;
                                add_data_to_dgv(text, Color.Black, Color.FromArgb(Convert.ToInt32(color)));
                            }
                            else
                            {
                                if (responseData.StartsWith("$%INFO:"))
                                {
                                    if (responseData.StartsWith("$%INFO:_del_at"))
                                    {
                                        if (responseData.StartsWith("$%INFO:_del_at,-1"))
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
                                                Refresh();
                                                dataGridView1.Rows.RemoveAt(Convert.ToInt32(responseData.Split(new char[] { ',' })[1]));
                                            });
                                        }
                                    }
                                    else
                                    {
                                        if (responseData.StartsWith("$%INFO:_edit_at"))
                                        {
                                            string[] infos = responseData.Split(new string[] { ",", ";$%OVERHEAD$%:" }, StringSplitOptions.RemoveEmptyEntries);

                                            int row_index = Convert.ToInt32(infos[1]);
                                            string new_text = infos[2];
                                            Color new_color = Color.FromArgb(Convert.ToInt32(infos[3]));

                                            dataGridView1.Rows[row_index].Cells[0].Value = new_text;
                                            dataGridView1.Rows[row_index].Cells[0].Style.BackColor = new_color;
                                        }
                                    }

                                }

                            }

                        }
                    }
                }


                catch (ArgumentNullException e)
                {
                    Console.WriteLine("ArgumentNullException: {0}", e);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
                catch (Exception ed)
                {
                    Console.WriteLine(ed.Message);
                }

                Thread.Sleep(10);
            }
        }


        bool items_saved = false;
        string speichern_unter_pfad;



        private void button1_Click(object sender, EventArgs e)
        {
            if (!is_started)
            {

                if (Connect())
                {
                    recive = new System.Threading.Thread(new System.Threading.ThreadStart(listen));
                    recive.Start();

                    button1.Text = "SENDEN";
                    is_started = true;

                }
                else
                {

                }
            }
            else
            {
                if (editmode)
                {
                    editmode = false;
                    if (dataGridView1.CurrentCell != null)
                    {

                        //if ((dataGridView1.CurrentCell.ColumnIndex == e.Cell.ColumnIndex) && dataGridView1.CurrentCell.RowIndex == e.Cell.RowIndex)
                        //{
                            dataGridView1.CurrentCell.ReadOnly = true;
                            int edit_index = dataGridView1.CurrentCell.RowIndex;
                            Senden("$%INFO:_edit_at," + edit_index.ToString() + "," + dataGridView1.CurrentCell.Value.ToString() + ";$%OVERHEAD$%:" + panel1.BackColor.ToArgb() + Environment.NewLine);

                            panel1.BackColor = last_panel_backcolor;

                        //}
                    }
                }
                else
                {


                    Senden_color(textBox1.Text, panel1.BackColor.ToArgb());
                    textBox1.Text = string.Empty;

                }

            }
        }


        private void add_data_to_dgv(string data, Color forecolor, Color backcolor)
        {
            if (!string.IsNullOrEmpty(data))
            {

                // string[] lines = data.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                //this.Invoke((MethodInvoker)delegate
                //{
                //    listView1.Items.Clear();
                //});



                this.Invoke((MethodInvoker)delegate
                {
                    dataGridView1.Rows.Add(data);

                    dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.BackColor = backcolor;

                    dataGridView1.CurrentCell = null;


                    //li.Text = data;
                    //li.ForeColor = forecolor;
                    //li.BackColor = backcolor;
                    //li.SubItems.Add(data, forecolor, backcolor,li.Font);
                    //listView1.Items.Add(li);
                    //s1.Width = this.Width;



                });



            }
        }




        public void Senden_color(string data, int color)
        {


            data = data + ";$%OVERHEAD$%:" + color+Environment.NewLine;


            Byte[] b_data = System.Text.Encoding.UTF8.GetBytes(data);
            stream.Write(b_data, 0, b_data.Length);


        }


        public void Senden(string data)
        {

            Byte[] b_data = System.Text.Encoding.UTF8.GetBytes(data + Environment.NewLine);
            stream.Write(b_data, 0, b_data.Length);

        }


        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
           

        }






        private void einstellungenToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Settings settings = new Settings(is_started, SERVERADDRESS, SERVERPORT);

            settings.ShowDialog();
            SERVERADDRESS = settings.IP;
            SERVERPORT = settings.PORT;
            //if (!is_started)
            //{
            //    client = new TcpClient(SERVERADD, SERVERPORT);
            //}
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void tSMI_del_Click(object sender, EventArgs e)
        {

            int deled_index = dataGridView1.CurrentCell.RowIndex;
            Senden("$%INFO:_del_at," + deled_index.ToString() + "," + MY_ADDRESS);


        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();

            panel1.BackColor = colorDialog1.Color;
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void alleEinträgeEntfernenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show("Sind Sie sicher, dass Sie alle Einträge löschen wollen?", "Alle Einträge löschen?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                Senden("$%INFO:_del_at,-1," + MY_ADDRESS);
               
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void label1_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();

            panel1.BackColor = colorDialog1.Color;
        }


        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (items_saved)
            {
                this.Close();
            }
            else
            {
                DialogResult m_result = MessageBox.Show(this, "Die aktuell angezeigten Meldungen sind nicht gespeichert!" + Environment.NewLine + "Möchten Sie siese jetzt speichern?", "Warnung", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);


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
                speichern_unter_pfad = null;

            }
            else
            {
                DialogResult m_result = MessageBox.Show(this, "Die aktuell angezeigten Meldungen sind nicht gespeichert!" + Environment.NewLine + "Möchten Sie siese jetzt speichern?", "Warnung", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);


                if (m_result == DialogResult.Yes)
                {
                    speichern_unter();
                    items_saved = true;
                    Senden("$%INFO:_del_at,-1");
                    speichern_unter_pfad = null;

                }
                else if (m_result == DialogResult.No)
                {

                    Senden("$%INFO:_del_at,-1");
                    items_saved = true;
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
                DialogResult m_result = MessageBox.Show(this, "Die aktuell angezeigten Meldungen sind nicht gespeichert!" + Environment.NewLine + "Möchten Sie siese jetzt speichern?", "Warnung", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);


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
                MessageBox.Show("Beim Soeichern der Datei ist ein Fehler aufgetreten.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    using (StreamReader sr = new StreamReader(openFileDialog1.FileName))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string[] data = sr.ReadLine().Split(new string[] { ";$%OVERHEAD$%:" }, StringSplitOptions.None);

                            string text = data[0];
                            string color = data[1];

                            Senden_color(text, Convert.ToInt32(color));
                        }
                        sr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beim Laden der Datei ist ein Fehler aufgetreten.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Senden_color(textBox1.Text, panel1.BackColor.ToArgb());
                textBox1.Text = string.Empty;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right&& dataGridView1.CurrentCell!=null)
            {
                int currentMouseOverRow = dataGridView1.HitTest(e.X, e.Y).RowIndex;
                int currentMouseOverColumn = dataGridView1.HitTest(e.X, e.Y).ColumnIndex;

                dataGridView1.CurrentCell = dataGridView1.Rows[currentMouseOverRow].Cells[currentMouseOverColumn];

                if (currentMouseOverRow >= 0)
                {
                    CMS1.Show(dataGridView1, new Point(e.X, e.Y));
                }


            }
        }

        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            dataGridView1.CurrentCell = null;

        }

        private void dataGridView1_Leave(object sender, EventArgs e)
        {
            dataGridView1.CurrentCell = null;

        }

        bool editmode = false;
        bool edit_is_on = false;
        Color last_panel_backcolor;

        private void eintragBearbeitenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //int edit_index = dataGridView1.CurrentCell.RowIndex;

            //Senden("$%INFO:_edit_at," + edit_index.ToString() + "," + MY_ADDRESS);
            last_panel_backcolor = panel1.BackColor;
            panel1.BackColor = dataGridView1.CurrentCell.Style.BackColor;


            dataGridView1.CurrentCell.ReadOnly = false;
            dataGridView1.BeginEdit(true);
            editmode = true;
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.CurrentCell != null&&edit_is_on==false)
            {

                edit_is_on = true;
                if ((dataGridView1.CurrentCell.ColumnIndex == e.ColumnIndex) && dataGridView1.CurrentCell.RowIndex == e.RowIndex)
                {
                    dataGridView1.CurrentCell.ReadOnly = true;
                    int edit_index = dataGridView1.CurrentCell.RowIndex;
                    Senden("$%INFO:_edit_at," + edit_index.ToString() + ","+dataGridView1.CurrentCell.Value.ToString()+ ";$%OVERHEAD$%:" + panel1.BackColor.ToArgb() + Environment.NewLine);
                    panel1.BackColor = last_panel_backcolor;
                }
            }
            edit_is_on = false;

        }

        private void dataGridView1_CellStateChanged(object sender, DataGridViewCellStateChangedEventArgs e)
        {
            if (editmode)
            {
                editmode = false;
                if (dataGridView1.CurrentCell != null && edit_is_on == false)
                {
                    edit_is_on = true;
                    if ((dataGridView1.CurrentCell.ColumnIndex == e.Cell.ColumnIndex) && dataGridView1.CurrentCell.RowIndex == e.Cell.RowIndex)
                    {
                        dataGridView1.CurrentCell.ReadOnly = true;
                        int edit_index = dataGridView1.CurrentCell.RowIndex;
                        Senden("$%INFO:_edit_at," + edit_index.ToString() + "," + dataGridView1.CurrentCell.Value.ToString() + ";$%OVERHEAD$%:" + panel1.BackColor.ToArgb() + Environment.NewLine);

                        panel1.BackColor = last_panel_backcolor;


                    }
                }
            }
            edit_is_on = false;

        }
    }
}