using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenGlove_API_C_Sharp_HL;
using System.Diagnostics;
using System.Threading;

namespace TimeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            testMany(4);
            //testOne();
        }

        static void testOne() {
            OpenGloveAPI api = OpenGloveAPI.GetInstance();
            var gloves = api.Devices;
            foreach (var glove in gloves)
            {
                for (int i = 0; i < 1001; i++)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Restart();
                    api.Activate(glove, (int)PalmarRegion.FingerIndexDistal, 255);
                    sw.Stop();
                    System.IO.File.AppendAllText(@"C:\Users\Sebastian\Documents\Tesis\pruebas\CS-API.txt", (sw.ElapsedTicks * 1000000 / Stopwatch.Frequency).ToString() + "\r\n");
                    Console.WriteLine("Test pass: UP " + i);
                    Thread.Sleep(250);

                    sw.Restart();
                    api.Activate(glove, (int)PalmarRegion.FingerIndexDistal, 0);
                    sw.Stop();
                    System.IO.File.AppendAllText(@"C:\Users\Sebastian\Documents\Tesis\pruebas\CS-API.txt", (sw.ElapsedTicks * 1000000 / Stopwatch.Frequency).ToString() + "\r\n");
                    Console.WriteLine("Test pass: DOWN " + i);
                    Thread.Sleep(250);
                }
                break;
            }

        }

        static void testMany(int amount) {
            OpenGloveAPI api = OpenGloveAPI.GetInstance();
            var gloves = api.Devices;

            List<int> regions = new List<int>();

            List<int> upInit = new List<int>();

            List<int> downInit = new List<int>();

            for (int i = 0; i < amount; i++)
            {
                regions.Add(i);
                upInit.Add(255);
                downInit.Add(0);
            }
            
            //thumb
            regions.Add((int) PalmarRegion.FingerThumbDistal);
            upInit.Add(255);
            downInit.Add(0);
            
            //palm
            regions.Add((int)PalmarRegion.ThenarIndex);
            upInit.Add(255);
            downInit.Add(0);
            
            foreach (var glove in gloves)
            {
                for (int i = 0; i < 1000; i++)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Restart();
                    api.Activate(glove, regions, upInit);
                    sw.Stop();
                    System.IO.File.AppendAllText(@"C:\Users\Sebastian\Documents\Tesis\pruebas\CS-API.txt", (sw.ElapsedTicks * 1000000 / Stopwatch.Frequency).ToString() + "\r\n");
                    Console.WriteLine("Test pass: UP " + i);
                    Thread.Sleep(250);

                    sw.Restart();
                    api.Activate(glove, regions, downInit);
                    sw.Stop();
                    System.IO.File.AppendAllText(@"C:\Users\Sebastian\Documents\Tesis\pruebas\CS-API.txt", (sw.ElapsedTicks * 1000000 / Stopwatch.Frequency).ToString() + "\r\n");
                    Console.WriteLine("Test pass: DOWN " + i);
                    Thread.Sleep(250);
                }
                break;
            }
        }
    }
}
