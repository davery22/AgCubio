﻿// Created by Daniel Avery and Keeton Hodgson
// November 2015

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using System.Drawing;

namespace AgCubio
{
    /// <summary>
    /// The world model for AgCubio
    /// </summary>
    public class World
    {
        // ---------------------- WORLD ATTRIBUTES ----------------------

        /// <summary>
        /// Width of the world (integer 0-)
        /// </summary>
        public readonly int WORLD_WIDTH;

        /// <summary>
        /// Height of the world (integer 0-)
        /// </summary>
        public readonly int WORLD_HEIGHT;

        /// <summary>
        /// Number of updates the server attemps to execute per second (integer 0-)
        /// </summary>
        public readonly int HEARTBEATS_PER_SECOND;

        // -------------------- FOOD/VIRUS ATTRIBUTES -------------------

        /// <summary>
        /// Default mass of food cubes (integer 0-)
        /// </summary>
        public readonly int FOOD_MASS;

        /// <summary>
        /// Default width of food cubes
        /// </summary>
        public readonly double FOOD_WIDTH;

        /// <summary>
        /// Default mass of viruses (integer 0-)
        /// </summary>
        public readonly int VIRUS_MASS;

        /// <summary>
        /// Default width of viruses
        /// </summary>
        public readonly double VIRUS_WIDTH;

        /// <summary>
        /// Percent of food that becomes a virus (integer 0-100)
        /// </summary>
        public readonly int VIRUS_PERCENT;

        /// <summary>
        /// Maximum total food cubes in the world (integer 0-)
        /// </summary>
        public readonly int MAX_FOOD_COUNT;

        // ---------------------- PLAYER ATTRIBUTES ---------------------

        /// <summary>
        /// Starting mass for all players (integer 0-)
        /// </summary>
        public readonly int PLAYER_START_MASS;

        /// <summary>
        /// Starting width of players
        /// </summary>
        public readonly double PLAYER_START_WIDTH;

        /// <summary>
        /// Maximum player speed - for small cube sizes (integer 0-10)
        /// </summary>
        public readonly double MAX_SPEED;

        /// <summary>
        /// Minimum player speed - for large cube sizes (integer 0-10)
        /// </summary>
        public readonly double MIN_SPEED;

        /// <summary>
        /// Constant in the linear equation for calculating a cube's speed
        /// </summary>
        public readonly double SPEED_CONSTANT;

        /// <summary>
        /// Slope in the linear equation for calculating a cubes's speed
        /// </summary>
        public readonly double SPEED_SLOPE;

        /// <summary>
        /// Scaler for how quickly a player cube loses mass (double 0-1)
        /// </summary>
        public readonly double ATTRITION_RATE;

        /// <summary>
        /// Minimum mass before a player can spit (integer 0-)
        /// </summary>
        public readonly int MIN_SPLIT_MASS;

        /// <summary>
        /// How far a cube can be thrown when split (integer 0-)
        /// </summary>
        public readonly int MAX_SPLIT_DISTANCE;

        /// <summary>
        /// Maximum total cubes a single player is allowed (integer 0-)
        /// </summary>
        public readonly int MAX_SPLIT_COUNT;

        /*/// <summary>
        /// Distance between cubes before a larger eats a smaller
        /// </summary>
        public readonly double ABSORB_DISTANCE_DELTA;*/

        // ---------------------- OTHER ATTRIBUTES ----------------------

        /// <summary>
        /// Dictionary for storing all the cubes. Uid's map to cubes
        /// </summary>
        public Dictionary<int, Cube> Cubes;

        /// <summary>
        /// Keeps track of all food.
        /// </summary>
        public HashSet<Cube> Food;

        /// <summary>
        /// Dictionary for tracking split cubes
        /// </summary>
        private Dictionary<int, HashSet<int>> SplitCubeUids;

        /// <summary>
        /// Our Uid counter
        /// </summary>
        private int Uid;

        /// <summary>
        /// Previously used Uid's that can now be reused (cubes were deleted)
        /// </summary>
        private Stack<int> Uids;

        /// <summary>
        /// Random number generator
        /// </summary>
        private Random Rand;

        // --------------------------------------------------------------


        /// <summary>
        /// Constructs a new world of the specified dimensions in the xml file
        /// </summary>
        public World(string filename)
        {
            SplitCubeUids = new Dictionary<int, HashSet<int>>();
            Cubes = new Dictionary<int, Cube>();
            Food = new HashSet<Cube>();
            Rand = new Random();
            Uids = new Stack<int>();
            
            using (XmlReader reader = XmlReader.Create(filename))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "width":
                                reader.Read();
                                int.TryParse(reader.Value, out this.WORLD_WIDTH);
                                break;

                            case "height":
                                reader.Read();
                                int.TryParse(reader.Value, out this.WORLD_HEIGHT);
                                break;

                            case "max_split_distance":
                                reader.Read();
                                int.TryParse(reader.Value, out this.MAX_SPLIT_DISTANCE);
                                break;

                            case "max_speed":
                                reader.Read();
                                double.TryParse(reader.Value, out this.MAX_SPEED);
                                break;

                            case "min_speed":
                                reader.Read();
                                double.TryParse(reader.Value, out this.MIN_SPEED);
                                break;

                            case "attrition_rate":
                                reader.Read();
                                double.TryParse(reader.Value, out this.ATTRITION_RATE);
                                break;

                            case "food_mass":
                                reader.Read();
                                int.TryParse(reader.Value, out this.FOOD_MASS);
                                break;

                            case "player_start_mass":
                                reader.Read();
                                int.TryParse(reader.Value, out this.PLAYER_START_MASS);
                                break;

                            case "max_food_count":
                                reader.Read();
                                int.TryParse(reader.Value, out this.MAX_FOOD_COUNT);
                                break;

                            case "min_split_mass":
                                reader.Read();
                                int.TryParse(reader.Value, out this.MIN_SPLIT_MASS);
                                break;

                            case "max_split_count":
                                reader.Read();
                                int.TryParse(reader.Value, out this.MAX_SPLIT_COUNT);
                                break;

                            /*case "absorb_constant":
                                reader.Read();
                                double.TryParse(reader.Value, out this.ABSORB_DISTANCE_DELTA);
                                break;*/

                            case "heartbeats_per_second":
                                reader.Read();
                                int.TryParse(reader.Value, out this.HEARTBEATS_PER_SECOND);
                                break;

                            case "virus_percent":
                                reader.Read();
                                int.TryParse(reader.Value, out this.VIRUS_PERCENT);
                                break;

                            case "virus_mass":
                                reader.Read();
                                int.TryParse(reader.Value, out this.VIRUS_MASS);
                                break;
                        }
                    }
                }
            }

            this.PLAYER_START_WIDTH = Math.Sqrt(this.PLAYER_START_MASS);
            this.FOOD_WIDTH = Math.Sqrt(this.FOOD_MASS);
            this.VIRUS_WIDTH = Math.Sqrt(this.VIRUS_MASS);

            double maxSpeedMass = 1 / (10 * ATTRITION_RATE);
            this.SPEED_SLOPE = (MIN_SPEED - MAX_SPEED) / (maxSpeedMass - PLAYER_START_MASS);
            this.SPEED_CONSTANT = (MAX_SPEED - MIN_SPEED * (PLAYER_START_MASS / maxSpeedMass)) / (1 - PLAYER_START_MASS / maxSpeedMass);

            while (this.Food.Count < this.MAX_FOOD_COUNT)
                this.GenerateFoodorVirus();
        }


        /// <summary>
        /// Default constructor: uses predetermined values
        /// </summary>
        public World()
        {
            SplitCubeUids = new Dictionary<int, HashSet<int>>();
            Cubes = new Dictionary<int, Cube>();
            Food = new HashSet<Cube>();
            Rand = new Random();
            Uids = new Stack<int>();

            //this.ABSORB_DISTANCE_DELTA = 5;
            this.ATTRITION_RATE = .0005;
            this.FOOD_MASS = 1;
            this.FOOD_WIDTH = Math.Sqrt(this.FOOD_MASS);
            this.HEARTBEATS_PER_SECOND = 30;
            this.WORLD_HEIGHT = 1000;
            this.WORLD_WIDTH = 1000;
            this.MAX_FOOD_COUNT = 5000;
            this.MAX_SPEED = 1;
            this.MAX_SPLIT_COUNT = 15;
            this.MAX_SPLIT_DISTANCE = 30;
            this.MIN_SPEED = 0.4;
            this.MIN_SPLIT_MASS = 25;
            this.PLAYER_START_MASS = 10;
            this.PLAYER_START_WIDTH = Math.Sqrt(this.PLAYER_START_MASS);
            this.VIRUS_MASS = 30;
            this.VIRUS_PERCENT = 2;
            this.VIRUS_WIDTH = Math.Sqrt(this.VIRUS_MASS);

            double maxSpeedMass = 1 / (10 * ATTRITION_RATE);
            this.SPEED_SLOPE = (MIN_SPEED - MAX_SPEED) / (maxSpeedMass - PLAYER_START_MASS);
            this.SPEED_CONSTANT = (MAX_SPEED - MIN_SPEED * (PLAYER_START_MASS / maxSpeedMass)) / (1 - PLAYER_START_MASS / maxSpeedMass);

            while (this.Food.Count < this.MAX_FOOD_COUNT)
                this.GenerateFoodorVirus();
        }


        /// <summary>
        /// Serializes all cubes in the world to send them to a new player.
        /// </summary>
        public string SerializeAllCubes()
        {
            StringBuilder info = new StringBuilder();
            foreach (Cube c in Food)
                info.Append(JsonConvert.SerializeObject(c) + "\n");

            info.Append(SerializePlayers());

            return info.ToString();
        }


        /// <summary>
        /// Serializes all players.
        /// </summary>
        public string SerializePlayers()
        {
            StringBuilder players = new StringBuilder();

            foreach (Cube c in Cubes.Values)
                players.Append(JsonConvert.SerializeObject(c) + "\n");

            return players.ToString();
        }


        /// <summary>
        /// Atrophy! Players decrease in size
        /// </summary>
        public void PlayerAttrition()
        {
            foreach (Cube c in Cubes.Values)
                if ((c.Mass * (1 - ATTRITION_RATE)) > this.PLAYER_START_MASS)
                    c.Mass *= (1 - this.ATTRITION_RATE);
        }


        /// <summary>
        /// Collisions
        /// c1 is the player cube
        /// c2 is food
        /// </summary>
        /// <returns></returns>
        private bool Collide(Cube c1, Cube c2)
        {
            if (c2.loc_x > c1.left && c2.loc_x < c1.right && c2.loc_y > c1.top && c2.loc_y < c1.bottom)
                return true;
            return false;
        }


        /// <summary>
        /// Manages cubes colliding against each other
        /// </summary>
        public string ManageCollisions()
        {
            StringBuilder destroyed = new StringBuilder();
            List<Cube> eatenFood;
            List<int> eatenPlayers = new List<int>();

            List<int> cuids = new List<int>(Cubes.Keys);

            for (int i = 0; i < cuids.Count; i++)
            {
                Cube player = Cubes[cuids[i]];
                if (player == null)
                    break;
                eatenFood = new List<Cube>();
                if (player.Mass == 0)
                    continue;

                foreach (Cube food in Food)
                {
                    if (Collide(player, food) && player.Mass > food.Mass)
                    {
                        if (food.Mass == VIRUS_MASS)
                        {
                            VirusSplit(player.uid, food.loc_x + 10, food.loc_y + 10);
                        }
                        else
                        player.Mass += food.Mass;

                        // Adjust cube position if edges go out of bounds
                        AdjustPosition(player.uid);

                        food.Mass = 0;
                        destroyed.Append(JsonConvert.SerializeObject(food) + "\n");
                        Uids.Push(food.uid);
                        eatenFood.Add(food);
                    }
                }

                IEnumerator<Cube> numerator2 = Cubes.Values.GetEnumerator();
                for (int j = i + 1; j < cuids.Count; j++)
                {
                    Cube players = Cubes[cuids[j]];
                    if (player.Mass == 0 || players.Mass == 0)
                        continue;


                    if (Collide(player, players) || Collide(players, player))
                    {
                        // IF TEAMID = UID and COUNTDOWN < 0, then the player can eat its own split cube

                        if (player.Team_ID != 0 && player.Team_ID == players.Team_ID)
                        {

                            //if countdown
                            if (players.uid == players.Team_ID)
                            {
                                players.Mass += player.Mass;
                                player.Mass = 0;
                                AdjustPosition(players.uid);

                                Uids.Push(player.uid);
                                eatenPlayers.Add(player.uid);
                                SplitCubeUids.Remove(player.uid);
                                destroyed.Append(JsonConvert.SerializeObject(player) + "\n");
                            }
                            else
                            {
                            player.Mass += players.Mass;
                            players.Mass = 0;
                                AdjustPosition(player.uid);

                                Uids.Push(players.uid);
                                eatenPlayers.Add(players.uid);
                                SplitCubeUids.Remove(players.uid);
                            destroyed.Append(JsonConvert.SerializeObject(players) + "\n");
                            }

                        }
                        else if (player.Mass > players.Mass)
                        {
                            int id = players.uid;
                            player.Mass += players.Mass;
                            players.Mass = 0;
                            AdjustPosition(player.uid);

                            if (players.uid == players.Team_ID && SplitCubeUids.ContainsKey(players.Team_ID))
                                id = ReassignUid(players.uid);
                            else
                                Uids.Push(players.uid);

                            eatenPlayers.Add(id);
                            destroyed.Append(JsonConvert.SerializeObject(players) + "\n");
                        }
                        else
                        {
                            int id = player.uid;

                            players.Mass += player.Mass;
                            player.Mass = 0;
                            AdjustPosition(players.uid);

                            if (player.uid == player.Team_ID && SplitCubeUids.ContainsKey(players.Team_ID))
                                id = ReassignUid(player.uid);
                            else
                                Uids.Push(player.uid);

                            eatenPlayers.Add(id);
                            destroyed.Append(JsonConvert.SerializeObject(player) + "\n");
                        }
                        }
                    }

                // Remove eaten food and players.
                foreach (Cube c in eatenFood)
                    Food.Remove(c);
            }
                foreach (int i in eatenPlayers)
                    Cubes.Remove(i);

            return destroyed.ToString();
            }


        private int ReassignUid(int cubeUid)
        {
            List<int> temp = new List<int>(SplitCubeUids[cubeUid]);
            int tempID = temp[1];
            SplitCubeUids[cubeUid].GetEnumerator().Dispose();
            Cubes[cubeUid].uid = tempID;
            Cubes[tempID].uid = cubeUid;

            SplitCubeUids.Remove(tempID);
            return tempID;

        }


        /// <summary>
        /// 
        /// </summary>
        private void AdjustPosition(int uid)
        {
            Cube player = Cubes[uid];
            if (player.left < 0)
                player.loc_x -= player.left;
            else if (player.right > this.WORLD_WIDTH)
                player.loc_x -= player.right - this.WORLD_WIDTH;
            if (player.top < 0)
                player.loc_y -= player.top;
            else if (player.bottom > this.WORLD_HEIGHT)
                player.loc_y -= player.bottom - this.WORLD_HEIGHT;
        }


        /// <summary>
        /// Creates a unit vector out of the given x and y coordinates
        /// </summary>
        public static void UnitVector(ref double x, ref double y)
        {
            double scale = Math.Sqrt(x * x + y * y);
            x /= scale;
            y /= scale;
        }


        /// <summary>
        /// Finds starting coordinates for a new player (or virus) cube so that it isn't immediately consumed (or exploded)
        /// </summary>
        public void FindStartingCoords(out double x, out double y, bool virus)
        {
            double width = virus ? VIRUS_WIDTH : PLAYER_START_WIDTH;

            // Assign random coordinates
            x = Rand.Next((int)width, WORLD_WIDTH - (int)width);
            y = Rand.Next((int)width, WORLD_HEIGHT - (int)width);

            // Retry if coordinates are contained by any other player cube
            foreach (Cube player in Cubes.Values)
                if ((x > player.left && x < player.right) && (y < player.bottom && y > player.top))
                    FindStartingCoords(out x, out y, virus);
        }


        /// <summary>
        /// Helper method: creates a unique uid to give a cube
        /// </summary>
        public int GetUid()
        {
            return (Uids.Count > 0) ? Uids.Pop() : Uid++;
        }


        /// <summary>
        /// Gives the cube a nice, vibrant, visible color
        /// </summary>
        public int GetColor()
        {
            return ~(Rand.Next(Int32.MinValue, Int32.MaxValue) & 0xf0f0f0);
        }


        /// <summary>
        /// Adds a new food cube to the world
        /// </summary>
        public Cube GenerateFoodorVirus()
            {
            // On a random scale needs to create viruses too (5% of total food? Less?)
            // Viruses: specific color, specific size or size range. I'd say a size of ~100 or so.
            // Cool thought: viruses can move, become npc's that can try to chase players, or just move erratically

            //Another thought: randomly allow a food piece to get 1 size bigger (mass++) each time this is called.

            int random = Rand.Next(100);
            int color, mass, width;
            double x, y;

            // Create a virus some percent of the time
            if (random < VIRUS_PERCENT)
            {
                color = Color.LightGreen.ToArgb();
                mass = VIRUS_MASS;
                width = (int)VIRUS_WIDTH;
                FindStartingCoords(out x, out y, true);
            }
            // Otherwise create food
            else
            {
                color = GetColor();
                mass = (random > 99) ? FOOD_MASS * 2 : FOOD_MASS; // 1% of food is double-size
                x = Rand.Next((int)FOOD_WIDTH, WORLD_WIDTH - (int)FOOD_WIDTH);
                y = Rand.Next((int)FOOD_WIDTH, WORLD_HEIGHT - (int)FOOD_WIDTH);
            }

            Cube foodOrVirus = new Cube(x, y, GetUid(), true, "", mass, color, 0);
            Food.Add(foodOrVirus);
            return foodOrVirus;
        }


        /// <summary>
        /// Controls a cube's movements
        /// </summary>
        public void Move(int PlayerUid, double x, double y)
        {
            if (SplitCubeUids.ContainsKey(PlayerUid) && SplitCubeUids[PlayerUid].Count > 0)
            {
                foreach (int uid in SplitCubeUids[PlayerUid])
                {
                    if (!Cubes.ContainsKey(uid))
                        continue;
                    double x0 = Cubes[uid].loc_x;
                    double y0 = Cubes[uid].loc_y;

                    MoveCube(uid, x, y);

                    foreach (int team in SplitCubeUids[PlayerUid])
                    {
                        if (uid == team)
                            continue;
                        if (!Cubes.ContainsKey(team))
                            continue;
                        CheckOverlap(uid, Cubes[team], x0, y0);
                    }
                }
            }
            else
                MoveCube(PlayerUid, x, y);
        }


        /// <summary>
        /// Helper method - checks for overlap between split cubes and cancels the directional movement that causes overlap
        /// </summary>
        public void CheckOverlap(int movingUid, Cube teammate, double x0, double y0)
        {
            Cube moving = Cubes[movingUid];

            if (((moving.left < teammate.right && moving.left > teammate.left) || (moving.right < teammate.right && moving.right > teammate.left)) &&
                ((moving.top < teammate.bottom && moving.top > teammate.top) || (moving.bottom < teammate.bottom && moving.bottom > teammate.top)))
            {
                double relative = Math.Abs(moving.loc_x - teammate.loc_x) - Math.Abs(moving.loc_y - teammate.loc_y);

                if (relative < 0)
                    Cubes[movingUid].loc_y = y0;
                else if (relative > 0)
                    Cubes[movingUid].loc_x = x0;
                else
                {
                    Cubes[movingUid].loc_x = x0;
                    Cubes[movingUid].loc_y = y0;
                }
            }
        }


        /// <summary>
        /// Helper method for move, moves split cubes as well
        /// TODO: Needs to check for boundaries of cubes, not allow them to occupy the same spaces.
        /// </summary>
        private void MoveCube(int CubeUid, double x, double y)
        {
            // Store cube width
            if (!Cubes.ContainsKey(CubeUid))
                return;

            // Get the actual cube
            Cube cube = Cubes[CubeUid];
            double cubeWidth = Cubes[CubeUid].width;

            // Get the relative mouse position:
            x -= cube.loc_x;
            y -= cube.loc_y;

            // If the mouse is in the very center of the cube, then don't do anything.
            if (Math.Abs(x) < 1 && Math.Abs(y) < 1)
                return;

            double speed = GetSpeed(CubeUid);

            // Normalize and scale the vector:
            UnitVector(ref x, ref y);
            x *= speed;
            y *= speed;

            // Set the new position
            Cubes[CubeUid].loc_x += (cube.left + x < 0 || cube.right + x > this.WORLD_WIDTH)   ? 0 : x;
            Cubes[CubeUid].loc_y += (cube.top + y < 0  || cube.bottom + y > this.WORLD_HEIGHT) ? 0 : y;
        }


        /// <summary>
        /// Gets the speed of the cube
        /// </summary>
        public double GetSpeed(int CubeUid)
        {
            Cube cube = Cubes[CubeUid];

            //if (cube.Team_ID == 0)
            //{
                double speed = SPEED_SLOPE * Cubes[CubeUid].Mass + SPEED_CONSTANT;
                return speed = (speed < MIN_SPEED) ? MIN_SPEED : ((speed > MAX_SPEED) ? MAX_SPEED : speed);
            //}
            //else
            //{

            //}
        }


        /// <summary>
        /// Manages split requests
        /// </summary>
        public void Split(int CubeUid, double x, double y)
        {
            if (!SplitCubeUids.ContainsKey(CubeUid))
            {
                if (Cubes[CubeUid].Mass < this.MIN_SPLIT_MASS)
                    return;
                Cubes[CubeUid].Team_ID = CubeUid;
                SplitCubeUids[CubeUid] = new HashSet<int>() { CubeUid };
            }
                

            List<int> temp = new List<int>(SplitCubeUids[CubeUid]);
            List<int> remove = new List<int>();
            foreach (int uid in temp)
            {
                if (!Cubes.ContainsKey(uid))
                {
                    remove.Add(uid);
                    continue;
                }

                if (SplitCubeUids[CubeUid].Count >= this.MAX_SPLIT_COUNT)
                    continue;

                double mass = Cubes[uid].Mass;
                if (mass < this.MIN_SPLIT_MASS)
                    continue;

                // Halve the mass of the original cube, create a new cube
                Cubes[uid].Mass = mass / 2;

                Cube newCube = new Cube(x, y, GetUid(), false, Cubes[CubeUid].Name, mass / 2, Cubes[CubeUid].argb_color, CubeUid);

                // Add the new cube to the world
                Cubes.Add(newCube.uid, newCube);

                SplitCubeUids[CubeUid].Add(newCube.uid);
            }

            foreach (int id in remove)
                if (!Cubes.ContainsKey(id))
                    SplitCubeUids[CubeUid].Remove(id);
        }

        /// <summary>
        /// Manages splitting when hit a virus
        /// Non optimal code.
        /// </summary>
        public void VirusSplit(int CubeUid, double x, double y)
        {
            if (!SplitCubeUids.ContainsKey(CubeUid))
            {
                Cubes[CubeUid].Team_ID = CubeUid;
                SplitCubeUids[CubeUid] = new HashSet<int>() { CubeUid };
            }

            if (SplitCubeUids[CubeUid].Count >= this.MAX_SPLIT_COUNT)
                return;

            double mass = Cubes[CubeUid].Mass;

            // Halve the mass of the original cube, create a new cube
            Cubes[CubeUid].Mass = (mass - 10) / 2;

            Cube newCube = new Cube(x + MAX_SPLIT_DISTANCE, y + MAX_SPLIT_DISTANCE, GetUid(), false, Cubes[CubeUid].Name, mass / 8, Cubes[CubeUid].argb_color, CubeUid);
            Cube newCube2 = new Cube(x - MAX_SPLIT_DISTANCE, y - MAX_SPLIT_DISTANCE, GetUid(), false, Cubes[CubeUid].Name, mass / 8, Cubes[CubeUid].argb_color, CubeUid);
            Cube newCube3 = new Cube(x - MAX_SPLIT_DISTANCE, y + MAX_SPLIT_DISTANCE, GetUid(), false, Cubes[CubeUid].Name, mass / 8, Cubes[CubeUid].argb_color, CubeUid);
            Cube newCube4 = new Cube(x + MAX_SPLIT_DISTANCE, y - MAX_SPLIT_DISTANCE, GetUid(), false, Cubes[CubeUid].Name, mass / 8, Cubes[CubeUid].argb_color, CubeUid);


            // Add the new cube to the world
            Cubes.Add(newCube.uid, newCube);
            Cubes.Add(newCube2.uid, newCube2);
            Cubes.Add(newCube3.uid, newCube3);
            Cubes.Add(newCube4.uid, newCube4);
            
            //Adjust position so its inside world.
            AdjustPosition(newCube.uid);
            AdjustPosition(newCube2.uid);
            AdjustPosition(newCube3.uid);
            AdjustPosition(newCube4.uid);



            SplitCubeUids[CubeUid].Add(newCube.uid);
            SplitCubeUids[CubeUid].Add(newCube2.uid);
            SplitCubeUids[CubeUid].Add(newCube3.uid);
            SplitCubeUids[CubeUid].Add(newCube4.uid);
        }


        /// <summary>
        /// THIS MAY OR MAY NOT BE USED TO TRACK COUNTDOWNS AND INERTIA OF A SPLIT CUBE
        /// Cube needs to gracefully slide to new position.
        /// </summary>
        class SplitCubeData
        {
            /// <summary>
            /// Unit vector direction
            /// </summary>
            public Tuple<double, double> direction;

            /// <summary>
            /// Speed that it is going at- needs to decrease quick with time
            /// </summary>
            public int inertia
            {
                get { return inertia--; }
                set { }
            }
            
            /// <summary>
            /// Countdown until it can merge again
            /// </summary>
            public int countdown
            {
                get { return countdown--; }
                set { }
            }

            /// <summary>
            /// 
            /// </summary>
            public int CubeUid;

            /// <summary>
            /// Passed in so that member variables can be accessed.
            /// </summary>
            World Parent;

            /* Design notes:
            Has an initial direction, multiplied by an initial scalar that decreases quickly. Mouse directions after this initial are added on to it, with their speed.
            Has a max distance of where the cube can split to- that split needs to happen quickly, and quickly scale back down to normal speed once that spot is attained.
            Everything needs to happen gradually.
            Work out the storing of data here, and the actual moving in the Move() method in the World class.

            This data structure needs to be stored in the hash set that right now just stores the splitcubeuid's.

            */

            /// <summary>
            /// 
            /// </summary>
            public SplitCubeData(double x, double y, World parent, int cubeUid)
            {
                inertia = parent.MAX_SPLIT_DISTANCE;//initial speed
                countdown = 200; //decremented each tick
                Parent = parent;
                int dist = Parent.MAX_SPLIT_DISTANCE;
                CubeUid = cubeUid;

                x -= Parent.Cubes[CubeUid].loc_x;
                y -= Parent.Cubes[CubeUid].loc_y;

                inertia = 2;//-(1 * Parent.Cubes[CubeUid].Mass / 58) + 59 / 58; // NEED A NEW SPEED SCALAR

                UnitVector(ref x, ref y);
                direction = new Tuple<double, double>(x, y);
            }
        }
    }
}


