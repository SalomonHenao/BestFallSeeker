using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BestFallSeeker
{
    class Program
    {
        //Main program
        static void Main()
        {
            try
            {
                //Setup variables
                string fileName = "map.txt";
                char separator = ' ';

                //Create stopwatch to control elapsed time
                Stopwatch timer = new Stopwatch();
                timer.Start();

                Console.Clear();
                Console.WriteLine("\nProcess started.\nPlease wait...");

                //Loads file data to memory
                List<List<int>> mountainMap = Tools.GetFileData(fileName, separator);

                //Calls the best fall calculator and stops timer after it returns a path
                List<CoordinateDto> bestFall = Tools.CalculateBestPath(mountainMap);
                timer.Stop();

                //Prints results
                if (bestFall != null)
                {
                    Logs.printResult(bestFall, mountainMap, timer.Elapsed.TotalSeconds);
                }
                else
                {
                    Logs.printError($"There seems to be an issue with the provided data.\nNo valid paths were found.");
                }
            }
            catch(Exception ex)
            {
                Logs.printError("\nPlease check the input dataset and the script setup.");
                Logs.printError($"Error details: {ex.Message}");
            }

        }
    }
}
