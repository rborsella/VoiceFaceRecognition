using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Diagnostics;

using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using System.Globalization; // recognition


namespace MultiFaceRec
{
    public partial class FrmPrincipal : Form
    {
        static SpeechSynthesizer ss = new SpeechSynthesizer();
        static SpeechRecognitionEngine sre;
        static bool done = false;
        static bool speechOn = true;
        

        public void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string txt = e.Result.Text;
            float confidence = e.Result.Confidence; // consider implicit cast to double
            richTextBox1.AppendText("\nRecognized: " + txt);

            if (confidence < 0.60) return;

            if (txt.IndexOf("speech on") >= 0)
            {
                richTextBox1.AppendText("Speech is now ON");
                speechOn = true;
            }

            if (txt.IndexOf("speech off") >= 0)
            {
                richTextBox1.AppendText("Speech is now OFF");
                speechOn = false;
            }

            if (speechOn == false) return;

            if (txt.IndexOf("klatu") >= 0 && txt.IndexOf("barada") >= 0)
            {
                ((SpeechRecognitionEngine)sender).RecognizeAsyncCancel();
                done = true;
                richTextBox1.AppendText("(Speaking: Farewell)");
                ss.Speak("Farewell");
            }

            if (txt.IndexOf("Who") >= 0 && txt.IndexOf("I") >= 0)
            {
                if (!String.Equals(label4.Text, ", "))
                {
                    ss.Speak("You are called, " + label4.Text);
                }
                else
                {
                    ss.Speak("I do not recognize you. What do you call yourself?");
                }
            }

            if (txt.IndexOf("My") >= 0 && txt.IndexOf("name") >= 0)
            {
                string[] words = txt.Split(' ');
                string myName = words[3];
                textBox1.Text = words[3];
                addPerson();
                ss.Speak("Hello," + myName + ", Nice to meet you.");

            }

            if (txt.IndexOf("Open") >= 0 && txt.IndexOf("Chrome") >= 0)
            {
                var procChrome = Process.Start("Chrome.exe", "http://www.google.com");
            }

            if (txt.IndexOf("Close") >= 0 && txt.IndexOf("Chrome") >= 0)
            {
                foreach (var process in Process.GetProcessesByName("Chrome"))
                {
                    process.Kill();
                }
            }

            if (txt.IndexOf("Tell") >= 0 && txt.IndexOf("joke") >= 0)
            {
                string[] jokes = new string[4];
                jokes[0] = "Did you hear about the new restaurant called karma? , They have no menu, you get what you deserve.";
                jokes[1] = "Ask me about loom.";
                jokes[2] = "How many I.T. guys does it take to screw in a light bulb? None, thats a facilities problem.";
                jokes[3] = "I dont always test my code, but when I do, its in production";
                Random jNum = new Random();
                int jokeNum = jNum.Next(0, 4);
                ss.Speak(jokes[jokeNum]);
            }

            if (txt.IndexOf("What") >= 0 && txt.IndexOf("loom") >= 0)
            {
                ss.Speak("You mean the latest masterpiece of fantasy storytelling from Lucasfilms Brian Moriarty? Why its an extraordinary adventure with an interface on magic... stunning, high-resolution, 3D landscapes... sophisticated score and musical effects. Not to mention the detailed animation and special effects, elegant point n click control of characters, objects, and magic spells. Beat the rush! Go out and buy Loom today!");
            }

            if (txt.IndexOf("What") >= 0 && txt.IndexOf("plus") >= 0) // what is 2 plus 3
            {
                string[] words = txt.Split(' ');     // or use e.Result.Words
                int num1 = int.Parse(words[2]);
                int num2 = int.Parse(words[4]);
                int sum = num1 + num2;
                richTextBox1.AppendText("(Speaking: " + words[2] + " plus " + words[4] + " equals " + sum + ")");
                ss.SpeakAsync(words[2] + " plus " + words[4] + " equals " + sum);
            }
        } // sre_SpeechRecognized
        //Declararation of all variables, vectors and haarcascades
        Image<Bgr, Byte> currentFrame;
        Capture grabber;
        HaarCascade face;
        HaarCascade eye;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        Image<Gray, byte> result, TrainedFace = null;
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels= new List<string>();
        List<string> NamePersons = new List<string>();
        int ContTrain, NumLabels, t;
        string name, names = null;


        public FrmPrincipal()
        {
            InitializeComponent();
            //Load haarcascades for face detection
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            //eye = new HaarCascade("haarcascade_eye.xml");
            try
            {
                //Load of previus trainned faces and labels for each image
                string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTrain = NumLabels;
                string LoadFaces;

                for (int tf = 1; tf < NumLabels+1; tf++)
                {
                    LoadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }
            
            }
            catch(Exception e)
            {
                //MessageBox.Show(e.ToString());
                MessageBox.Show("Nothing in binary database, please add at least a face(Simply train the prototype with the Add Face Button).", "Triained faces load", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            ss.SetOutputToDefaultAudioDevice();
            richTextBox1.AppendText("\n(Speaking: I am awake)");

            CultureInfo ci = new CultureInfo("en-us");
            sre = new SpeechRecognitionEngine(ci);
            sre.SetInputToDefaultAudioDevice();
            sre.SpeechRecognized += sre_SpeechRecognized;

            Choices ch_StartStopCommands = new Choices();
            ch_StartStopCommands.Add("speech on");
            ch_StartStopCommands.Add("speech off");
            ch_StartStopCommands.Add("Who am I");
            ch_StartStopCommands.Add("My name is");
            ch_StartStopCommands.Add("Open Chrome");
            ch_StartStopCommands.Add("Close Chrome");
            ch_StartStopCommands.Add("What is loom");
            ch_StartStopCommands.Add("Tell me a joke");
            ch_StartStopCommands.Add("klatu barada nikto");
            GrammarBuilder gb_StartStop = new GrammarBuilder();
            gb_StartStop.Append(ch_StartStopCommands);
            Grammar g_StartStop = new Grammar(gb_StartStop);

            Choices ch_NameExchange = new Choices();
            ch_NameExchange.Add("Shane");
            ch_NameExchange.Add("Richard");
            ch_NameExchange.Add("Latrese");
            ch_NameExchange.Add("John");
            ch_NameExchange.Add("Michael");
            ch_NameExchange.Add("Linda");
            ch_NameExchange.Add("Ron");
            ch_NameExchange.Add("Marie");
            ch_NameExchange.Add("Alan");
            ch_NameExchange.Add("Joe");
            ch_NameExchange.Add("Stefanie");
            ch_NameExchange.Add("Chris");
            GrammarBuilder gb_NameExchange = new GrammarBuilder();
            gb_NameExchange.Append("My name is");
            gb_NameExchange.Append(ch_NameExchange);
            Grammar g_NameExchange = new Grammar(gb_NameExchange);

            //string[] numbers = new string[] { "1", "2", "3", "4" };
            //Choices ch_Numbers = new Choices(numbers);

            //string[] numbers = new string[100];
            //for (int i = 0; i < 100; ++i)
            //  numbers[i] = i.ToString();
            //Choices ch_Numbers = new Choices(numbers);

            Choices ch_Numbers = new Choices();
            ch_Numbers.Add("1");
            ch_Numbers.Add("2");
            ch_Numbers.Add("3");
            ch_Numbers.Add("4"); // technically Add(new string[] { "4" });

            //for (int num = 1; num <= 4; ++num)
            //{
            //  ch_Numbers.Add(num.ToString());
            //}

            GrammarBuilder gb_WhatIsXplusY = new GrammarBuilder();
            gb_WhatIsXplusY.Append("What is");
            gb_WhatIsXplusY.Append(ch_Numbers);
            gb_WhatIsXplusY.Append("plus");
            gb_WhatIsXplusY.Append(ch_Numbers);
            Grammar g_WhatIsXplusY = new Grammar(gb_WhatIsXplusY);

            sre.LoadGrammarAsync(g_StartStop);
            sre.LoadGrammarAsync(g_WhatIsXplusY);
            sre.LoadGrammarAsync(g_NameExchange);

            sre.RecognizeAsync(RecognizeMode.Multiple); // multiple grammars

        }


        private void button1_Click(object sender, EventArgs e)
        {
            //Initialize the capture device
            grabber = new Capture();
            grabber.QueryFrame();
            //Initialize the FrameGraber event
            Application.Idle += new EventHandler(FrameGrabber);
            button1.Enabled = false;
        }


        public void addPerson()
        {
            try
            {
                //Trained face counter
                ContTrain = ContTrain + 1;

                //Get a gray frame from capture device
                gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                //Face Detector
                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                face,
                1.2,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));

                //Action for each element detected
                foreach (MCvAvgComp f in facesDetected[0])
                {
                    TrainedFace = currentFrame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                //resize face detected image for force to compare the same size with the 
                //test image with cubic interpolation type method
                TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                trainingImages.Add(TrainedFace);
                labels.Add(textBox1.Text);

                //Show face added in gray scale
                imageBox1.Image = TrainedFace;

                //Write the number of triained faces in a file text for further load
                File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                //Write the labels of triained faces in a file text for further load
                for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(Application.StartupPath + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }

            }
            catch
            {
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        void FrameGrabber(object sender, EventArgs e)
        {
            label3.Text = "0";
            //label4.Text = "";
            NamePersons.Add("");


            //Get the current frame form capture device
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                    //Convert it to Grayscale
                    gray = currentFrame.Convert<Gray, Byte>();

                    //Face Detector
                    MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                  face,
                  1.2,
                  10,
                  Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                  new Size(20, 20));

                    //Action for each element detected
                    foreach (MCvAvgComp f in facesDetected[0])
                    {
                        t = t + 1;
                        result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        //draw the face detected in the 0th (gray) channel with blue color
                        currentFrame.Draw(f.rect, new Bgr(Color.Red), 2);


                        if (trainingImages.ToArray().Length != 0)
                        {
                            //TermCriteria for face recognition with numbers of trained images like maxIteration
                        MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.001);

                        //Eigen face recognizer
                        EigenObjectRecognizer recognizer = new EigenObjectRecognizer(
                           trainingImages.ToArray(),
                           labels.ToArray(),
                           3000,
                           ref termCrit);

                        name = recognizer.Recognize(result);

                            //Draw the label for each face detected and recognized
                        currentFrame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

                        }

                            NamePersons[t-1] = name;
                            NamePersons.Add("");


                        //Set the number of faces detected on the scene
                        label3.Text = facesDetected[0].Length.ToString();
                       
                        /*
                        //Set the region of interest on the faces
                        
                        gray.ROI = f.rect;
                        MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
                           eye,
                           1.1,
                           10,
                           Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                           new Size(20, 20));
                        gray.ROI = Rectangle.Empty;

                        foreach (MCvAvgComp ey in eyesDetected[0])
                        {
                            Rectangle eyeRect = ey.rect;
                            eyeRect.Offset(f.rect.X, f.rect.Y);
                            currentFrame.Draw(eyeRect, new Bgr(Color.Blue), 2);
                        }
                         */

                    }
                        t = 0;

                        //Names concatenation of persons recognized
                    for (int nnn = 0; nnn < facesDetected[0].Length; nnn++)
                    {
                        names = names + NamePersons[nnn] + ", ";
                    }
                    //Show the faces procesed and recognized
                    imageBoxFrameGrabber.Image = currentFrame;
                    label4.Text = names;
                    names = "";
                    //Clear the list(vector) of names
                    NamePersons.Clear();

                }
    }
}