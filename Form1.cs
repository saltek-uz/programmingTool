using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace programmingTool
{
    public partial class Form1 : Form
    {
        const uint POLYNOMIAL = 0x8408;
        SerialPort sp = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);

        byte[] txBuff = new byte[128];
        byte[] rxBuff = new byte[128];

        bool OKreceived = false;

        static byte[] fWare = new byte[65536];

        public Form1()
        {
            sp.DataReceived += new SerialDataReceivedEventHandler(onReceive);
            InitializeComponent();
        }




        void onReceive(object sender, SerialDataReceivedEventArgs e)
        {
            if (sp.BytesToRead < 2) return;

            string ss = sp.ReadExisting();

            if (ss == "OK") { ss = " Усшешно ! \r\n "; OKreceived = true; }
            if (ss == "ER") { ss = " Ошибка ! \r\n "; OKreceived = false; }


            //textBox1.Invoke(new Action(() => {textBox1.Text += ss;})); 




        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            if (comboBox1.Items.Count > 0) { comboBox1.SelectedIndex = 0; };
        }


        uint GetCRC16(byte[] bufData, int sizeData)
        {
            uint TmpCRC, i;
            uint j;
            TmpCRC = 0;
            for (i = 0; i < sizeData; i++)
            {
                TmpCRC = TmpCRC ^ bufData[i];
                for (j = 0; j < 8; j++)
                {
                    if ((TmpCRC & 0x0001) != 0) { TmpCRC >>= 1; TmpCRC ^= POLYNOMIAL; }
                    else TmpCRC >>= 1;
                }
            }
            return TmpCRC;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            txBuff[0] = 0x02; txBuff[1] = 0x03; txBuff[2] = 0x06; txBuff[3] = 0xBB;
            uint crc = GetCRC16(txBuff, 4);
            txBuff[4] = (byte)(crc & 0x00FF); txBuff[5] = (byte)((crc >> 8) & 0x00FF);
            sp.Write(txBuff, 0, 6);
        }

        private void ENTER_PRG_Click(object sender, EventArgs e)
        {
            OKreceived = false;
            textBox1.Text += " Попытка подключения к купюрнику ... \r\n ";
            sp.BaudRate = 9600;
            txBuff[0] = 0x02; txBuff[1] = 0x03; txBuff[2] = 0x06; txBuff[3] = 0x88;
            uint crc = GetCRC16(txBuff, 4);
            txBuff[4] = (byte)(crc & 0x00ff); txBuff[5] = (byte)((crc >> 8) & 0x00ff);
            sp.Write(txBuff, 0, 6);
            Thread.Sleep(500);

            if (OKreceived) textBox1.Text += " Усшешно ! \r\n ";
            else textBox1.Text += " Ошибка ! \r\n ";




            //  sp.BaudRate = 115200;

        }


        void writePage(byte[] page, int adr)
        {

            byte addrHi = (byte)(adr >> 8);
            byte addrLo = (byte)(adr & 0xff);

            txBuff[0] = 0x02; txBuff[1] = 0x03; // Suffix
            txBuff[2] = 0x48;                   // 72 byte to write
            txBuff[3] = 0xAA;                   // writePage CMD

            txBuff[4] = addrHi;                   // ADDR high
            txBuff[5] = addrLo;                   // ADDR low

            for (int i = 0; i < 64; i++) txBuff[i + 6] = page[i];

            uint crc = GetCRC16(txBuff, 70);

            txBuff[70] = (byte)(crc & 0x00FF);
            txBuff[71] = (byte)((crc >> 8) & 0x00FF);

            sp.Write(txBuff, 0, 72);
        }

        private void button2_Click(object sender, EventArgs e)
        {



            OKreceived = false;
            write_1C00();
            textBox1.Text += "1C00 line Unlocked...";
            Thread.Sleep(500);
            if (OKreceived) textBox1.Text += " Усшешно ! \r\n ";
            else { textBox1.Text += " Ошибка ! \r\n "; return; };


            OKreceived = false;
            write_1A00();
            textBox1.Text += "1A00 line Unlocked... ";
            Thread.Sleep(500);
            if (OKreceived) textBox1.Text += " Усшешно ! \r\n ";
            else { textBox1.Text += " Ошибка ! \r\n "; return; };

            OKreceived = false;
            write_1E00();
            textBox1.Text += "1E00 line Unlocked... ";
            Thread.Sleep(500);
            if (OKreceived) textBox1.Text += " Усшешно ! \r\n ";
            else { textBox1.Text += " Ошибка ! \r\n "; return; };





            if (write_Boot())
            {
                textBox1.Text += "Загрузчик разблокирован! \r\n";

            }
            else
            {
                textBox1.Text += "Сбой при разблокировке загрузчика\r\n";
                return;
            };



            OKreceived = false;

            textBox1.Text += "FFFF vector line Unlocked... ";
            write_FE00();
            Thread.Sleep(500);
            if (OKreceived) textBox1.Text += " Усшешно ! \r\n ";
            else { textBox1.Text += " Ошибка ! \r\n "; return; };

            textBox1.Text += "нажмите Restart Chip \r\n";

        }

        bool write_Boot()
        {
            bool tp = true;

            string[] bltr =
        {
                "31 40 00 0A 3C 40 00 02 3D 40 4D 00 B0 12 7A 13 B0 12 58 12 B0 12 B8 13 5E 42 76 00 F2 E0 80 00 19 00 F2 90 03 00 4B 02 0F 28 5F 42 4B 02 4F 4F CF 4E 00 02 D2 53 4B 02 D2 92 4C 02 4B 02 29 28",
                "C2 43 4B 02 30 40 94 11 E2 93 4B 02 10 20 C2 4E 4C 02 D2 53 4B 02 D2 42 4C 02 02 02 4F 4E 7F 80 06 00 7F 90 4B 00 03 28 C2 43 4B 02 30 41 D2 93 4B 02 08 20 7E 90 03 00 03 20 D2 53 4B 02 02 3C",
                "C2 43 4B 02 C2 93 4B 02 04 20 6E 93 02 20 D2 53 4B 02 30 41 E2 43 00 02 F2 40 03 00 01 02 5D 42 02 02 4D 4D 3D 50 FE FF 3C 40 00 02 B0 12 A8 12 0F 4C 8F 10 3F F0 FF 00 5E 42 02 02 4E 4E CE 9C",
                "FE 01 19 20 5E 42 02 02 4E 4E CE 9F FF 01 01 24 30 41 5E 42 03 02 7E 80 99 00 07 24 7E 80 11 00 06 24 7E 80 11 00 05 24 30 41 30 40 0C 13 30 40 F8 11 B0 12 B0 13 30 41 0A 12 5F 42 05 02 4F 4F",

                "5A 42 04 02 4A 4A 3A F0 FF 00 8A 10 0A DF 3E 40 40 00 3D 40 00 02 0C 4A B0 12 E2 12 0E 43 01 3C 1E 53 3E 90 40 00 0E 34 0F 4A 0F 5E EE 9F 06 02 F7 27 7C 40 45 00 B0 12 56 13 7C 40 52 00 B0 12",
                "56 13 08 3C 7C 40 4F 00 B0 12 56 13 7C 40 4B 00 B0 12 56 13 3A 41 30 41 B0 12 00 1E B2 40 88 5A 20 01 F2 40 C4 00 57 00 F2 40 C0 00 58 00 C2 43 72 00 F2 40 10 00 70 00 F2 40 11 00 71 00 F2 40",
                "60 00 74 00 E2 43 75 00 F2 D0 C0 00 04 00 F2 40 30 00 1B 00 F2 40 44 00 19 00 F2 40 C4 00 1A 00 B0 12 34 13 0C 43 30 41 0A 12 0E 43 0A 43 01 3C 1A 53 0A 9D 13 2C 0F 4C 0F 5A 6F 4F 4F 4F 0E EF",
                "4F 43 03 3C 12 C3 0E 10 5F 53 7F 92 F1 2F 1E B3 F9 2B 12 C3 0E 10 3E E0 08 84 F6 3F 0C 4E 3A 41 30 41 B0 12 8A 13 B0 12 A0 13 7F 40 06 00 08 3C 4F 4F DC 4F 00 02 00 00 5F 53 1D 53 3E 53 1C 53",

                "0E 93 F6 23 B0 12 98 13 30 40 A8 13 5F 42 05 02 4F 4F 5C 42 04 02 4C 4C 3C F0 FF 00 8C 10 0C DF B0 12 68 13 7C 40 4F 00 B0 12 56 13 7C 40 4B 00 30 40 56 13 7C 40 4F 00 B0 12 56 13 7C 40 4B 00",
                "B0 12 56 13 C2 43 4B 02 F2 B0 40 00 02 00 FC 2B B0 12 18 11 F9 3F 0F 43 01 3C 1F 53 3F 90 C8 00 FC 3B C2 4C 77 00 30 41 B0 12 8A 13 B2 40 02 A5 28 01 CC 43 00 00 30 40 A8 13 0D 5C 03 3C CC 43",
                "00 00 1C 53 0C 9D FB 23 30 41 B2 40 82 A5 2A 01 B2 40 00 A5 2C 01 30 41 B2 40 00 A5 28 01 30 41 B2 40 40 A5 28 01 30 41 B2 40 10 A5 2C 01 30 41 B2 40 00 5A 20 01 30 41 30 40 BC 13 30 40 C0 13",
                "FF 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00",

                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00",
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00",
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00",
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00"
        };

            textBox1.Text += "Обновление загрузчика . ";

            for (int i = 0; i < 16; i++)
            {
                int adr = 0x2000 + i * 0x200;

                string tmp = bltr[i];
                string[] tts = tmp.Split(' ');
                byte[] tt = new byte[64];
                for (int j = 0; j < 64; j++)
                    tt[j] = byte.Parse(tts[j].Trim(), NumberStyles.HexNumber);

                OKreceived = false;
                writePage(tt, adr);
                textBox1.Text += " . ";
                Thread.Sleep(200);
                if (OKreceived == false) { tp = false; break; };

            }

            textBox1.Text += "\r\n";
            return tp;

        }


        void write_1C00()
        {

            /* erase
                    B2 40 88 5A 20 01 B2 40 82 A5 2A 01 B2 40 00 A5
                    2C 01 3F 40 00 11 B2 40 02 A5 28 01 CF 43 00 00
                    3F 40 00 13 B2 40 02 A5 28 01 CF 43 00 00 3F 40
                    00 15 B2 40 02 A5 28 01 CF 43 00 00 30 41 
            */

            byte[] tt =
                {
                    0xB2, 0x40, 0x88, 0x5A, 0x20, 0x01, 0xB2, 0x40, 0x82, 0xA5, 0x2A, 0x01, 0xB2, 0x40, 0x00, 0xA5,
                    0x2C, 0x01, 0x3F, 0x40, 0x00, 0x11, 0xB2, 0x40, 0x02, 0xA5, 0x28, 0x01, 0xCF, 0x43, 0x00, 0x00,
                    0x3F, 0x40, 0x00, 0x13, 0xB2, 0x40, 0x02, 0xA5, 0x28, 0x01, 0xCF, 0x43, 0x00, 0x00, 0x3F, 0x40,
                    0x00, 0x15, 0xB2, 0x40, 0x02, 0xA5, 0x28, 0x01, 0xCF, 0x43, 0x00, 0x00, 0x30, 0x41, 0x00, 0x00
                };
            writePage(tt, 0x1C00);
        }




        void write_1E00()
        {

            /* cmpnd
                    B0 12 00 1C B2 40 40 A5 28 01 3E 40 00 11 3D 40
                    00 20 0D 3C 0F 4E 3F F0 3F 00 0C 4D 0C 5F EE 4C
                    00 00 1E 53 7E B0 3F 00 02 20 3D 50 00 02 3E 90
                    00 15 F0 2B 30 40 00 1A 
             */


            byte[] tt =
                {
                     0xB0 , 0x12 , 0x00 , 0x1C , 0xB2 , 0x40 , 0x40 , 0xA5 , 0x28 , 0x01 , 0x3E , 0x40 , 0x00 , 0x11 , 0x3D , 0x40,
                     0x00 , 0x20 , 0x0D , 0x3C , 0x0F , 0x4E , 0x3F , 0xF0 , 0x3F , 0x00 , 0x0C , 0x4D , 0x0C , 0x5F , 0xEE , 0x4C,
                     0x00 , 0x00 , 0x1E , 0x53 , 0x7E , 0xB0 , 0x3F , 0x00 , 0x02 , 0x20 , 0x3D , 0x50 , 0x00 , 0x02 , 0x3E , 0x90,
                     0x00 , 0x15 , 0xF0 , 0x2B , 0x30 , 0x40 , 0x00 , 0x1A , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00
                };
            writePage(tt, 0x1E00);
        }



        void write_1A00()
        {
            /*addit
                        B2 40 82 A5 2A 01 B2 40 00 A5 2C 01 3F 40 00 FF
                        B2 40 02 A5 28 01 CF 43 00 00       B2 40 40 A5 28 01
                        C2 43 FE FF   F2 40 0B 00   FF FF B2 40   00 A5 28 01
                        B2 40 10 A5   2C 01 B2 40   00 5A 20 01   30 41 
            */

            byte[] tt =
                {
                    0xB2, 0x40, 0x82, 0xA5, 0x2A, 0x01, 0xB2, 0x40, 0x00, 0xA5, 0x2C, 0x01, 0x3F, 0x40, 0x00, 0xFF,
                    0xB2, 0x40, 0x02, 0xA5, 0x28, 0x01, 0xCF, 0x43, 0x00, 0x00, 0xB2, 0x40, 0x40, 0xA5, 0x28, 0x01,
                    0xC2, 0x43, 0xFE, 0xFF, 0xF2, 0x40, 0x11, 0x00, 0xFF, 0xFF, 0xB2, 0x40, 0x00, 0xA5, 0x28, 0x01,
                    0xB2, 0x40, 0x10, 0xA5, 0x2C, 0x01, 0xB2, 0x40, 0x00, 0x5A, 0x20, 0x01, 0x30, 0x41, 0x00, 0x00

                };
            writePage(tt, 0x1A00);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!sp.IsOpen) sp.Open();
            button1.Enabled = false;
            comboBox1.Enabled = false;

        }

        private void button4_Click(object sender, EventArgs e)
        {

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog(this) != DialogResult.OK) return;

            button5.Enabled = false;

            try
            {
                textBox1.Text += "Открываем файл обновления : " + openFileDialog1.FileName + "\r\n";

                var fileStream = openFileDialog1.OpenFile();

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string[] fileContent = reader.ReadToEnd().Split('\n');

                    for (int i = 0; i < 0xFFFF; i++) fWare[i] = 0xFF;

                    int crAdr = 0x1100;
                    foreach (string s in fileContent)
                    {
                        string ss = s.Trim();
                        if (ss[0] == '@')
                        {
                            ss = ss.Substring(1, 4);
                            crAdr = int.Parse(ss, NumberStyles.HexNumber);
                        }
                        else
                        {
                            if (ss[0] == 'q') break;
                            string[] tmp = ss.Split();
                            foreach (string hx in tmp)
                            {
                                string hh = hx.Trim();
                                fWare[crAdr++] = byte.Parse(hh, NumberStyles.HexNumber);
                            }
                        }
                    }
                }

                textBox1.Text += " Успешно! \r\n";
                button5.Enabled = true;
            }

            catch
            {
                MessageBox.Show("ошибка открытия файла!");
            };


        }


        void write_FE00()
        {

            byte[] tt =
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x31, 0x40, 0x00, 0x0A, 0x30, 0x40,
                    0x00, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1E

                };

            writePage(tt, 0xFFC0);
        }

        void erasePage(int adr)
        {
            byte addrHi = (byte)(adr >> 8);
            byte addrLo = (byte)(adr & 0xff);

            txBuff[0] = 0x02; txBuff[1] = 0x03; // Suffix
            txBuff[2] = 0x08;                   // 8 byte to write
            txBuff[3] = 0x99;                   // writePage CMD

            txBuff[4] = addrHi;                   // ADDR high
            txBuff[5] = addrLo;                   // ADDR low

            uint crc = GetCRC16(txBuff, 6);

            txBuff[6] = (byte)(crc & 0x00FF);
            txBuff[7] = (byte)((crc >> 8) & 0x00FF);

            sp.Write(txBuff, 0, 8);

        }

        private void button5_Click(object sender, EventArgs e)
        {

            // write 1300++ sections:
            byte[] tt = new byte[64];

            for (int i = 0x1500; i < 0xE000; i++)
            {

                if ((i & 0x01FF) == 0x100)
                {
                    OKreceived = false;
                    textBox1.Text += "0x" + i.ToString("X4") + " line erasing... ";
                    erasePage(i);
                    Thread.Sleep(300);
                    if (OKreceived) textBox1.Text += " Усшешно ! \r\n ";
                    else { textBox1.Text += " Ошибка ! \r\n "; return; };


                }

                //if ((i & 0x003F) == 0x003F)
                //{ // writePage
                //    for (int j = 0; j < 64; j++) tt[j] = fWare[i & 0xFFC0 + j];
                //    OKreceived = false;
                //    writePage(tt, i);
                //    textBox1.Text += "0x" + i.ToString("X4") + " line Unlocked... ";
                //    Thread.Sleep(300);
                //    if (OKreceived) textBox1.Text += " Усшешно ! \r\n ";
                //    else { textBox1.Text += " Ошибка ! \r\n "; return; };


                //}


            }







        }
    }




}
