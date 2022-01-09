using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

                //Loads raw data to memory
                List<List<int>> mountainMap = GetFileData(fileName, separator);

                //Calls best fall calculator and stops timer after it returns data
                List<Coordinate> bestFall = CalculateBestFall(mountainMap, timer);
                timer.Stop();

                //Print results
                if (bestFall != null)
                {
                    //Titles
                    Console.Clear();
                    Console.WriteLine("\nBest fall found:");
                    Console.WriteLine($"Elapsed: {Math.Round(timer.Elapsed.TotalSeconds, 2)} seconds");
                    Console.WriteLine($"Steps: {bestFall.Count}");
                    Console.WriteLine($"Drop: {GetMapValue(bestFall.First(), mountainMap) - GetMapValue(bestFall.Last(), mountainMap)}");
                    Console.WriteLine($"\nCoordinates:");

                    //Steps
                    for (int i = 0; i < bestFall.Count; i++)
                    {
                        string lineIndicator = i == 0 ? "Start"
                            : i == (bestFall.Count-1) ? "Finally"
                            : $"Step {i}";

                        Console.WriteLine($"{lineIndicator}: X{bestFall[i].row},Y{bestFall[i].col}: {GetMapValue(bestFall[i], mountainMap)}");
                    }
                }
                else
                {
                    Console.WriteLine($"There seems to be an issue with the provided data.\nNo valid falls were found.");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("\nPlease check the input dataset and the script setup.");
                Console.WriteLine($"Error details: {ex.Message}");
            }

        }

        //Loads file to memory
        //Used a list of lists to simplify the data handling
        private static List<List<int>> GetFileData(
            string fileName,
            char separator)
        {
            List<List<int>> fileData = new List<List<int>>();

            //Reads all bytes and uses stream reader to load data into object
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"\\{fileName}");
            byte[] byteArray = File.ReadAllBytes(filePath);
            using (var sreader = new StreamReader(new MemoryStream(byteArray)))
            {
                //Reads first row to get size
                List<int> size = new List<int>();
                try
                {
                    //Reades the size headers and casts them to int
                    size = sreader.ReadLine().Split(separator).Select(s => Convert.ToInt32(s)).ToList();
                }
                catch
                {
                    throw new Exception("\nThere is a problem with the size indication.\nThe first two columns of the first row must contain the matrix size.");
                }

                //Reads all the other rows
                while (!sreader.EndOfStream)
                {
                    List<int> row = sreader.ReadLine().Split(separator).Select(s => Convert.ToInt32(s)).ToList();
                    fileData.Add(row);
                }

                //Validates the headers and the data dimenssions by using them
                if(size.Count != 2
                    || fileData.Count != size.First()
                    || fileData.OrderBy(o => o.Count).First().Count != size.Last()
                    || fileData.OrderBy(o => o.Count).Last().Count != size.Last())
                {
                    throw new Exception("Data doesn't match with the given size.");
                }
            }
            return fileData;
        }

        //Calculates the best fall
        //The longest and tallest
        private static List<Coordinate> CalculateBestFall(
            List<List<int>> mountainMap,
            Stopwatch timer)
        {
            //Creates bag designed to store data incoming from parallel treads
            //All the feasible routes will be stored here during the analysis process
            ConcurrentBag<List<Coordinate>> feasiblePaths = new ConcurrentBag<List<Coordinate>>();

            //Starts status bar service to keep the console updated
            StatusBar(feasiblePaths, mountainMap);

            //Max parallelism of 10 threads
            ParallelOptions parallelismLimit = new ParallelOptions { MaxDegreeOfParallelism = 10 };

            //As we have two for loops, we'll examine 100 possible landing points at a time
            //The system checks all the possible landing points and their derivated routes
            Parallel.For(0, mountainMap.Count, parallelismLimit, rows =>
            {
                Parallel.For(0, mountainMap.First().Count, parallelismLimit, cols =>
               {
                   //Creates the first coordinate, the landing spot
                   Coordinate landingSite = new Coordinate { row = rows, col = cols };
                   //Starts an exploring tree that will follow all the possible routes
                   GetfeasiblePaths(new List<Coordinate>(),
                       landingSite, mountainMap, feasiblePaths);
               });
            });

            //Orders the concurrent bag and extracts the longest and tallest route
            List<Coordinate> bestFall = feasiblePaths
                        .OrderByDescending(o => o.Count)
                        .ThenByDescending(or =>
                            GetMapValue(or.First(), mountainMap)
                            -
                            GetMapValue(or.Last(), mountainMap))
                        .FirstOrDefault();

            return bestFall;
        }

        //Keeps the status bar updated while processing data
        private static void StatusBar(
            ConcurrentBag<List<Coordinate>> feasiblePaths,
            List<List<int>> mountainMap)
        {
            Task.Run(() =>
            {
                int status = 0;
                while(status < 100)
                {
                    //Calculates the amount of posible paths according to the defined rules
                    decimal posiblePaths = (8) //All 4 corners have 2 posible paths
                        + ((mountainMap.Count - 2) * 6) //Vertical edges have 3 posible paths
                        + ((mountainMap.First().Count - 2) * 6) //Horizontal edges have 3 posible paths
                        + ((mountainMap.Count - 2) * (mountainMap.First().Count - 2) * 4); //Rest of matrix has 4 posible paths
                    //Calculates the evaluated paths
                    decimal evaluatedPaths = feasiblePaths.Count;

                    //Calculates the status percentage based on the evaluated paths and the posible paths
                    int newStatus = Convert.ToInt32(100 * (evaluatedPaths / posiblePaths));

                    //Updates status bar when the value increases
                    if (newStatus > status)
                    {
                        status = newStatus;
                        string statusBar = new String('|', status / 2) + new String('-', 50 - (status / 2));
                        Console.Clear();
                        Console.WriteLine($"\nEvaluated {string.Format("{0:n0}", evaluatedPaths)}" +
                            $" of {string.Format("{0:n0}", posiblePaths)} posible falls..." +
                            $"\n\n{statusBar} > {status}%");
                    }
                    Thread.Sleep(200);
                }
            });
        }

        //Creates an exploring tree
        //Creates all the possible routes starting from a position
        private static void GetfeasiblePaths(
            List<Coordinate> history,
            Coordinate currentPosition,
            List<List<int>> mountainMap,
            ConcurrentBag<List<Coordinate>> feasiblePaths)
        {
            //History contains all the steps that a thread has followed since its beginin
            history = history.Select(s => new Coordinate { row = s.row, col = s.col }).ToList();

            //Every new call stores a new coordinate in a thread history
            //This enables the recopilation of all the feasible routes
            history.Add(new Coordinate { row = currentPosition.row, col = currentPosition.col });

            //If a thread arrives to a coordinate without an exit, it's time to store the route
            //If not, the thread must do another iteration over every available path to follow
            if (ArrivedToEnd(history, mountainMap))
            {
                //Adds the thread history to the feasible paths concurrent bag
                feasiblePaths.Add(history);
            }
            else
            {
                //Gets all the available paths to follow from the current coordinate
                List<Coordinate> availablePaths = GetAvailablePaths(currentPosition, mountainMap);

                //Creates a new iteration with every available path to follow
                foreach (Coordinate path in availablePaths)
                {
                    GetfeasiblePaths(history, path, mountainMap, feasiblePaths);
                }
            }
        }

        //Creates the available paths to follow from a given position
        private static List<Coordinate> GetAvailablePaths(
            Coordinate currentPosition,
            List<List<int>> mountainMap)
        {
            List<Coordinate> availablePaths = new List<Coordinate>();
            
            //Calculates left path
            availablePaths.Add(new Coordinate
            {
                row = currentPosition.row,
                col = currentPosition.col - 1
            });

            //Calculates right path
            availablePaths.Add(new Coordinate
            {
                row = currentPosition.row,
                col = currentPosition.col + 1
            });

            //Calculates up path
            availablePaths.Add(new Coordinate
            {
                row = currentPosition.row - 1,
                col = currentPosition.col
            });

            //Calculates down path
            availablePaths.Add(new Coordinate
            {
                row = currentPosition.row + 1,
                col = currentPosition.col
            });

            //Filters the paths to allow only those matching the rules
            //1. Can't exceed the matrix dimenssion
            //2. The current position value must be grather than the next position value
            availablePaths = availablePaths
                            .Where(w =>
                                w.row >= 0
                                && w.col >= 0
                                && w.row <= (mountainMap.Count() -1)
                                && w.col <= (mountainMap.First().Count() -1)
                                && GetMapValue(currentPosition, mountainMap) > GetMapValue(w, mountainMap))
                            .ToList();

            return availablePaths;
        }

        //Validates if no available paths for a given position
        private static bool ArrivedToEnd(
            List<Coordinate> history,
            List<List<int>> mountainMap)
        {
            List<Coordinate> pathOptions = GetAvailablePaths(history.Last(), mountainMap);
            if(pathOptions.Count() > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //Extracts a cell value from the map using XY coordinates
        private static int GetMapValue(
            Coordinate position,
            List<List<int>> mountainMap)
        {
            return mountainMap[position.row][position.col];
        }

        //Class used to navigate the matrix with XY coordinates
        private class Coordinate
        {
            public int row { get; set; }

            public int col { get; set; }
        }
    }
}
