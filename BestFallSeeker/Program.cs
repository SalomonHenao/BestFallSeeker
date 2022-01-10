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
                string filePath = "map.txt";
                char separator = ' ';

                //Start process
                Console.Clear();
                Console.WriteLine("\nProcess started.\nPlease wait...");

                //Create stopwatch to control elapsed time
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //Loads file data to memory
                List<List<int>> mountainMap = Tools.GetFileData(filePath, separator);

                //Calls the best fall calculator and stops timer after it returns a path
                List<CoordinateDto> bestPath = Tools.CalculateBestPath(mountainMap);
                timer.Stop();

                //Prints results
                if (bestPath != null)
                {
                    Logs.PrintResult(bestPath, mountainMap, timer.Elapsed.TotalSeconds);
                }
                else
                {
                    Logs.PrintError($"There seems to be an issue with the provided data.\nNo valid paths were found.");
                }
            }
            catch(Exception ex)
            {
                Logs.PrintError("\nPlease check the input dataset and the script setup.");
                Logs.PrintError($"Error details: {ex.Message}");
            }

        }
    }
}
