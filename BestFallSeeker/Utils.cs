using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BestFallSeeker
{
    //Contains the console handlers
    class Logs
    {
        //Helper to print colored errors
        public static void PrintError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = ConsoleColor.White;
        }

        //Helper to print colored success
        public static void PrintSuccess(string content)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(content);
            Console.ForegroundColor = ConsoleColor.White;
        }

        //Keeps the status bar updated while processing data
        public static void StatusBar(
            ConcurrentBag<List<CoordinateDto>> optionalPaths,
            List<List<int>> mountainMap)
        {
            Task.Run(() =>
            {
                //Calculates the amount of potential paths according to the defined rules
                decimal potentialPaths = (8) //All 4 corners have 2 potential paths
                    + ((mountainMap.Count - 2) * 6) //Vertical edges have 3 potential paths
                    + ((mountainMap.First().Count - 2) * 6) //Horizontal edges have 3 potential paths
                    + ((mountainMap.Count - 2) * (mountainMap.First().Count - 2) * 4); //Rest of matrix has 4 potential paths

                //Starts in 0% and runs until 100% is reached
                int status = 0;
                while (status < 100)
                {
                    //Calculates the already evaluated paths
                    decimal evaluatedPaths = optionalPaths.Count;

                    //Calculates the status percentage based on the evaluated paths and the potential paths
                    int newStatus = Convert.ToInt32(100 * (evaluatedPaths / potentialPaths));

                    //Updates the status bar when the value increases
                    if (newStatus > status)
                    {
                        status = newStatus;
                        Console.Clear();

                        //Prints details of the process status
                        Console.WriteLine($"\nProcessing {mountainMap.Count} * {mountainMap.First().Count} dataset..." +
                            $"\nEvaluated {string.Format("{0:n0}", evaluatedPaths)}" +
                            $" of {string.Format("{0:n0}", potentialPaths)} potential paths...");

                        //Prints the formatted status bar
                        string statusBar = new String('|', status / 2) + new String(' ', 50 - (status / 2));
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write($"\n{statusBar}");
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($" > {status}%");
                    }
                    Thread.Sleep(300);
                }
            });
        }

        //Prints the formatted result
        public static void PrintResult(
            List<CoordinateDto> bestPath,
            List<List<int>> mountainMap,
            double elapsedTime)
        {
            //Prints summary
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
            PrintSuccess("\nBest fall:\n");
            Console.WriteLine($"Steps: {bestPath.Count}");
            Console.WriteLine($"Drop: {Tools.GetMapValue(bestPath.First(), mountainMap) - Tools.GetMapValue(bestPath.Last(), mountainMap)}");
            Console.WriteLine($"Elapsed: {Math.Round(elapsedTime, 2)} seconds\n");

            //Prints path steps
            for (int i = 0; i < bestPath.Count; i++)
            {
                string lineIndicator = i == 0 ? "Start"
                    : i == (bestPath.Count - 1) ? "Finally"
                    : $"Step {i}";

                PrintSuccess($"{lineIndicator} > ");
                Console.Write($"X{bestPath[i].Row},Y{bestPath[i].Col} > ");
                PrintSuccess($"[{Tools.GetMapValue(bestPath[i], mountainMap)}]\n");
            }
        }
    }

    //Contains the system helpers
    class Tools
    {
        //Loads file to memory
        //Used a list of lists to simplify data handling
        public static List<List<int>> GetFileData(
            string filePath,
            char separator)
        {
            List<List<int>> fileData = new List<List<int>>();

            //Reads all bytes and uses stream reader to load data into object
            byte[] byteArray = File.ReadAllBytes(filePath);
            using (var sreader = new StreamReader(new MemoryStream(byteArray)))
            {
                //Reads first row to get size
                List<int> size = new List<int>();
                try
                {
                    //Reads size headers and casts them to integer
                    size = sreader.ReadLine().Split(separator).Select(s => Convert.ToInt32(s)).ToList();
                }
                catch
                {
                    throw new Exception("\nThere is a problem with the size indication.\nThe first two columns of the first row must contain the matrix size.");
                }

                //Reads all data rows
                while (!sreader.EndOfStream)
                {
                    List<int> row = sreader.ReadLine().Split(separator).Select(s => Convert.ToInt32(s)).ToList();
                    fileData.Add(row);
                }

                //Validates headers and data dimenssions
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

        //Calculates the best path
        //The longest and tallest
        public static List<CoordinateDto> CalculateBestPath(
            List<List<int>> mountainMap)
        {
            //Creates bag designed to store data incoming from parallel threads
            //All optional routes will be stored here during the analysis process
            ConcurrentBag<List<CoordinateDto>> optionalPaths = new ConcurrentBag<List<CoordinateDto>>();

            //Launches status bar service to keep the console updated
            Logs.StatusBar(optionalPaths, mountainMap);

            //Max parallelism of 10 threads
            ParallelOptions parallelismLimit = new ParallelOptions { MaxDegreeOfParallelism = 10 };

            //As we have two for loops, we'll examine 100 potential landing points at a time
            //The system checks all the potential landing points and their derivated routes
            Parallel.For(0, mountainMap.Count, parallelismLimit, rows =>
            {
                Parallel.For(0, mountainMap.First().Count, parallelismLimit, cols =>
               {
                   //Creates the first coordinate, the landing spot
                   CoordinateDto landingSite = new CoordinateDto { Row = rows, Col = cols };
                   //Starts an explorer tree that will follow all the potential routes
                   FollowPath(new List<CoordinateDto>(),
                       landingSite, mountainMap, optionalPaths);
               });
            });

            //Orders the concurrent bag and extracts the longest and tallest route
            List<CoordinateDto> bestPath = optionalPaths
                        .OrderByDescending(o => o.Count)
                        .ThenByDescending(or =>
                            GetMapValue(or.First(), mountainMap)
                            -
                            GetMapValue(or.Last(), mountainMap))
                        .FirstOrDefault();

            return bestPath;
        }

        //Creates a concurrent tree to explore all the derivated paths
        //Grows with the potential paths starting from a position
        public static void FollowPath(
            List<CoordinateDto> history,
            CoordinateDto currentPosition,
            List<List<int>> mountainMap,
            ConcurrentBag<List<CoordinateDto>> optionalPaths)
        {
            //History contains all the steps that a thread has followed
            history = history.Select(s => new CoordinateDto { Row = s.Row, Col = s.Col }).ToList();

            //Every new call stores a new coordinate in the history
            //This enables the recopilation of all the optional routes
            history.Add(new CoordinateDto { Row = currentPosition.Row, Col = currentPosition.Col });

            //Gets all the available paths to follow from the current coordinate
            List<CoordinateDto> availablePaths = GetAvailablePaths(currentPosition, mountainMap);

            //If a thread arrives to a coordinate without an exit, it's time to store the route
            //If not, the thread must do a new iteration with each available path to follow
            if (availablePaths.Count() > 0)
            {
                //Creates a new iteration with every available path to follow
                foreach (CoordinateDto path in availablePaths)
                {
                    FollowPath(history, path, mountainMap, optionalPaths);
                }
            }
            else
            {
                //Adds the thread history to the optional paths concurrent bag
                optionalPaths.Add(history);
            }
        }

        //Returns all the available paths to follow from a given position
        public static List<CoordinateDto> GetAvailablePaths(
            CoordinateDto currentPosition,
            List<List<int>> mountainMap)
        {
            List<CoordinateDto> availablePaths = new List<CoordinateDto>();
            
            //Calculates left path
            availablePaths.Add(new CoordinateDto
            {
                Row = currentPosition.Row,
                Col = currentPosition.Col - 1
            });

            //Calculates right path
            availablePaths.Add(new CoordinateDto
            {
                Row = currentPosition.Row,
                Col = currentPosition.Col + 1
            });

            //Calculates up path
            availablePaths.Add(new CoordinateDto
            {
                Row = currentPosition.Row - 1,
                Col = currentPosition.Col
            });

            //Calculates down path
            availablePaths.Add(new CoordinateDto
            {
                Row = currentPosition.Row + 1,
                Col = currentPosition.Col
            });

            //Filters the paths to allow only those matching the rules
            //1. Can't exceed the matrix dimenssion
            //2. The current position value must be greater than the next position value
            availablePaths = availablePaths
                            .Where(w =>
                                w.Row >= 0
                                && w.Col >= 0
                                && w.Row <= (mountainMap.Count() -1)
                                && w.Col <= (mountainMap.First().Count() -1)
                                && GetMapValue(currentPosition, mountainMap) > GetMapValue(w, mountainMap))
                            .ToList();

            return availablePaths;
        }

        //Extracts a cell value from the data map using XY coordinates
        public static int GetMapValue(
            CoordinateDto position,
            List<List<int>> mountainMap)
        {
            return mountainMap[position.Row][position.Col];
        }
    }
}
