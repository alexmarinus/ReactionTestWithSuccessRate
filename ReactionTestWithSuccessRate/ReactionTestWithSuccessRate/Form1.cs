using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data.OleDb;
using System.Xml.Serialization;
using System.IO;

namespace ReactionTestWithSuccessRate
{
    public partial class Form1 : Form
    {
        /* Date despre suprafata de joc si 
         * cercurile ce vor fi generate pentru
         * testul de reactie */
        // Grafica jocului
        private Graphics testAreaGraphics;
        // Lungimea si latimea suprafetei de joc
        private int testAreaWidth, testAreaHeight;
        
        /* Numarul de ordine al formei aparute
         * (0 - triunghi, 1 - patrat, 2 - cerc, 3 - hexagon) */
        private int drawnShapeNumber;

        // Numarul iteratiei curente
        private int currentIteration;

        // Cronometrul pentru o iteratie
        private Stopwatch stopwatch;

        // Numarul de iteratii incheiate cu succes
        private static int successIters, successRatio;

        // Lista casetelor de text cu timpii
        private List<TextBox> timesTextBoxes = new List<TextBox>();

        /* Lista colturilor stanga-jos ale imaginilor 
         * continand formele geometrice pentru testul de reactie */
        private List<Point> lowerLeftCornersShapes = new List<Point>();

        /* Dimensiunea unui forme si spatiul dintre forme */
        private static int SHAPE_SIZE = 100, SHAPE_GAP = 100;

        /* Constante pentru cele 4 forme */
        private static int TRIANGLE_ID = 0, BLUE_SQUARE_ID = 1, CIRCLE_ID = 2, RED_SQUARE_ID = 3;

        // Calea completa a fisierului cu baza de date
        private String fullDatabaseFilePath;
        // String pentru conexiunea cu baza de date
        private String connectionString;

        // Constructorul pentru aplicatie
        public Form1()
        {
            // Initializez aplicatia de test
            InitializeComponent();

            // Obtin grafica ei
            testAreaGraphics = pictureBox1.CreateGraphics();

            // Obtin dimensiunile suprafetei de test
            testAreaWidth = pictureBox1.Size.Width;
            testAreaHeight = pictureBox1.Size.Height;

            // Nu am inceput testul, deci nu am intrat in nicio iteratie
            currentIteration = -1;
            // Si nici nu s-a desenat vreo forma
            drawnShapeNumber = -1;

            // Initializez cronometrele
            stopwatch = new Stopwatch();

            // Specific ca desenele sa fie incadrate pe suprafata de test
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            // Initializez numarul de succese
            successIters = 0;

            // Modific lista casetelor de text cu timpi de reactie
            timesTextBoxes.Add(textBox1);
            timesTextBoxes.Add(textBox2);
            timesTextBoxes.Add(textBox3);
            timesTextBoxes.Add(textBox4);
            timesTextBoxes.Add(textBox5);

            /* Adaug punctele reprezentand colturile stanga-jos
             * ale imaginilor (in ordinea corespunzatoare) */
            // Pentru triunghi
            lowerLeftCornersShapes.Add(new Point (0, 0));
            // Pentru patrat albastru
            lowerLeftCornersShapes.Add(new Point((SHAPE_SIZE + SHAPE_GAP), 0));
            // Pentru cerc
            lowerLeftCornersShapes.Add(new Point(2 * (SHAPE_SIZE + SHAPE_GAP), 0));
            // Pentru patrat rosu
            lowerLeftCornersShapes.Add(new Point(3 * (SHAPE_SIZE + SHAPE_GAP), 0));

            // Initializari pentru conectarea la baza de date
            fullDatabaseFilePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName +
                "\\ReactionTestSuccessDatabase.accdb";

            /* String-ul principal folosit pentru stabilirea conexiunii
             * cu baza de date */
            connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fullDatabaseFilePath;

            /* Setez ascultatorul de evenimente de tastatura
             * si ma asigur ca acesta nu se efectueaza de 2 ori
             * la apasarea unei taste */
            this.KeyDown -= Form1_KeyDown;
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);

            // Precizez ca aplicatia sa poata asculta tastatura
            this.KeyPreview = true;
        }

        /* Obtinerea in mod aleator a numarului urmatoarei forme desenate */
        private int GetRandomShapeNumber() {
            Random randomizer = new Random();
            int randomShapeNumber = randomizer.Next(4);
            return randomShapeNumber;
        }

        /* Desenarea unei forme in functie de numarul de ordine */
        private void DrawShapeByNumber()
        {
            Image shapeImage = imageList1.Images[drawnShapeNumber];
            Point shapeImageLeftCorner = lowerLeftCornersShapes.ElementAt(drawnShapeNumber);
            testAreaGraphics.DrawImage(shapeImage, shapeImageLeftCorner);
        }

        /* Tratarea succesului sau esecului la iteratie cu triunghi */
        private void TreatShapeIteration(int shapeNumber)
        {
            Int32 reaction_time = 0;
            /* Daca numarul formei desenate este acelasi cu
             * cel pentru care s-a dat click */
            if (drawnShapeNumber == shapeNumber)
            {
                // Opresc cronometrul
                stopwatch.Stop();
                // Obtin timpul de reactie
                reaction_time = Convert.ToInt32(stopwatch.ElapsedMilliseconds);
                // Resetez cronometrul
                stopwatch.Reset();
                // Resetez cronometrul
                stopwatch.Reset();
                // Incerementez numarul de succese
                successIters++;
                // Actualizez caseta de text
                timesTextBoxes.ElementAt(currentIteration).Text = reaction_time.ToString();
            }
            else
            {
                // Opresc cronometrul
                stopwatch.Stop();
                // Resetez cronometrul
                stopwatch.Reset();
                // Marchez iteratia ca fiind nereusita
                timesTextBoxes.ElementAt(currentIteration).Text = "Failure";
            }
        }

        /* Metoda de calcul a mediei de succes pentru un jucator */
        private void ComputeSuccessRatio()
        {
            successRatio = (successIters * 100) / 5;

            textBox6.Text = successRatio.ToString();
        }

        /* Adaugarea rezultatului unui jucator in baza de date Access */
        private void AddPlayerResultToDatabase()
        {
            // Obtin data curenta
            DateTime currentDate = DateTime.Now;

            // Numele jucatorului
            String playerName = textBox8.Text;

            // Conexiunea propriu-zisa si configurarea ei
            OleDbConnection connection = new OleDbConnection();
            connection.ConnectionString = connectionString;

            // Comanda de introducere a rezultatului jucatorului
            OleDbCommand command = new OleDbCommand();
            // Nu am pus si ID deoarce este incrementat automat in tabel
            command.CommandText = "INSERT INTO Results (PlayerName, DateFinished, SuccessRatio)" +
                "VALUES (@PlayerName, @DateFinished, @SuccessRatio)";
            // Setarea conexiunii de utilizat pentru comanda
            command.Connection = connection;

            // Deschiderea conexiunii
            connection.Open();

            // Daca s-a stabilit conexiunea
            if (connection.State == ConnectionState.Open)
            {
                /* Atunci adaug parametrii necesari comenzii de mai sus
                 * (adica valorile caracteristice unui jucator) */
                command.Parameters.Add("@PlayerName", OleDbType.VarChar).Value = playerName;
                command.Parameters.Add("@DateFinished", OleDbType.Date).Value = currentDate;
                command.Parameters.Add("@SuccessRatio", OleDbType.VarChar).Value = Convert.ToInt32(textBox6.Text);

                try
                {
                    // Execut comanda
                    command.ExecuteNonQuery();
                    // Inchid conexiunea
                    connection.Close();
                }
                // In caz de exceptie
                catch (OleDbException ex)
                {
                    // Afisez mesajul aferent excpetiei
                    MessageBox.Show(ex.Message);
                    // Inchid conexiunea
                    connection.Close();
                }
            }
            // Altfel conexiunea a esuat
            else
                MessageBox.Show("Connection Failed");
        }

        /* Ascultator asociat butonului Best player overall */
        private void button1_Click(object sender, EventArgs e)
        {
            // Conexiunea propriu-zisa si configurarea ei
            OleDbConnection connection = new OleDbConnection();
            connection.ConnectionString = connectionString;

            // Deschiderea conexiunii
            connection.Open();

            // Textul comenzii de selectie
            String commandText = "SELECT * FROM Results WHERE SuccessRatio ="+
                " (SELECT MAX(SuccessRatio) FROM Results)";
            // Obiect asociat comenzii, creat pe baza textului si a conexiunii
            OleDbCommand command = new OleDbCommand(commandText, connection);

            // Executia comenzii
            var myReader = command.ExecuteReader();

            // Citirea rezultatului
            myReader.Read();

            // Preluarea valorilor campurilor din rezultat
            string playerName = myReader.GetString(1);
            DateTime dateFinished = myReader.GetDateTime(2);
            Int32 successRatio = myReader.GetInt32(3);

            // Inchiderea conexiunii si cititorului bazei de date
            myReader.Close();
            connection.Close();

            // Actualizarea rezultatului in caseta de text
            textBox7.Text = playerName + " ; " + dateFinished.ToString() + " ; " + successRatio + "%";
        }

        /* Metoda de tratare a unui click dreapta pentru start de joc */
        private void TreatRightClick()
        {
            String playerName;

            // Doar daca nu a inceput jocul
            if (currentIteration == -1)
            {
                // Obtin numele jucatorului
                playerName = textBox8.Text;
                // Verific daca este introdus
                if (String.IsNullOrEmpty(playerName))
                {
                    MessageBox.Show("Please enter your name !");
                    return;
                }
                // Trec la prima iteratie
                currentIteration++;
                // Setez campul de text al jucatorului ca fiind needitabil
                textBox8.Enabled = false;
                // Eliberez casetele de timpi
                ClearPlayerTimes();
                // Eliberez fundalul
                testAreaGraphics.Clear(Color.White);
                // Aleg aleator urmatorul tip de figura
                drawnShapeNumber = GetRandomShapeNumber();
                // Astept 5 secunde
                System.Threading.Thread.Sleep(5000);
                // Desenez figura
                DrawShapeByNumber();
                // Pornesc cronometrul
                stopwatch.Start();
            }
        }

        /* Eliberearea casetelor de text */
        private void ClearPlayerTimes()
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            textBox4.Clear();
            textBox5.Clear();
            textBox6.Clear();
            textBox7.Clear();
        }

        /* Ascultator pentru clickuri de mouse pe fereastra de aplicatie */
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // Preiau butonul de mouse apasat
            MouseButtons mouseButtonClicked = e.Button;

            // Doar daca nu a inceput jocul
            if (mouseButtonClicked == MouseButtons.Right)
                TreatRightClick();
        }

        /* Ascultator pentru clickuri de mouse pe suprafata de test */
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // Preiau butonul de mouse apasat
            MouseButtons mouseButtonClicked = e.Button;

            // Doar daca nu a inceput jocul
            if (mouseButtonClicked == MouseButtons.Right)
                TreatRightClick();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (currentIteration == -1)
                return;

            /* Daca am apasat A sau H atunci tratez
             * iteratia pentru triunghi */
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.H)
                TreatShapeIteration(TRIANGLE_ID);
            /* Daca am apasat S sau J atunci
             * tratez iteratia pentru patrat albastru */
            else if (e.KeyCode == Keys.S || e.KeyCode == Keys.J)
                TreatShapeIteration(BLUE_SQUARE_ID);
            /* Daca am apasat D sau K atunci
             * tratez iteratia pentru patrat albastru */
            else if (e.KeyCode == Keys.D || e.KeyCode == Keys.K)
                TreatShapeIteration(CIRCLE_ID);
            /* Daca am apasat F sau L atunci
             * tratez iteratia pentru patrat albastru */
            else if (e.KeyCode == Keys.F || e.KeyCode == Keys.L)
                TreatShapeIteration(RED_SQUARE_ID);
            /* Daca nu s-a apasat niciuna din taste,
             * atunci nu se intampla nimic */
            else return;

            // Trec la urmatoarea iteratie
            currentIteration++;

            // Daca testul de reactie s-a terminat
            if (currentIteration == 5)
            {
                // Resetez testul
                currentIteration = -1;
                // Calculez media timpilor
                ComputeSuccessRatio();
                // Resetez numarul de iteratii reusite
                successIters = 0;
                // Adaug rezultatul la BD
                AddPlayerResultToDatabase();
                // Eliberez fundalul
                testAreaGraphics.Clear(Color.White);
                // Resetez campul de nume ca fiind editabil
                textBox8.Enabled = true;
                return;
            }

            // Eliberez fundalul
            testAreaGraphics.Clear(Color.White);
            // Pauza de 2 secunde pana la urmatoarea iteratie
            System.Threading.Thread.Sleep(2000);
            // Aleg aleator urmatorul tip de figura
            drawnShapeNumber = GetRandomShapeNumber();
            // O desenez
            DrawShapeByNumber();
            // Si pornesc cronometrul
            stopwatch.Start();
        }
    }        
}
