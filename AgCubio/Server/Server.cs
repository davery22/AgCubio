﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AgCubio
{
    class Server
    {
        private Dictionary<string, Socket> NamesSockets;

        TcpListener TcpServer;

        private World World;

        private int Uid;

        private Stack<int> Uids;

        Random RandomNumber;

        private Timer Heartbeat;

        private StringBuilder DataReceived;

        private StringBuilder DataSent;


        static void Main(string[] args)
        {
            new Server();
            Console.ReadLine();
        }

        public Server()
        {
            World = new World(); //Use the file path later.
            NamesSockets = new Dictionary<string, Socket>();
            //new Thread(() => Network.Server_Awaiting_Client_Loop(new Network.Callback(SaveServer)));
            Network.Server_Awaiting_Client_Loop(new Network.Callback(SaveServer));
            System.Diagnostics.Debug.WriteLine("Hello from the server constructor.");


            Heartbeat = new Timer(HeartBeatTick, null, 0, 1000 / World.HEARTBEATS_PER_SECOND);

            //more work location

            Uids = new Stack<int>();
            RandomNumber = new Random();
            DataSent = new StringBuilder();
            DataReceived = new StringBuilder();
        }


        private void SaveServer(Preserved_State_Object state)
        {
            this.TcpServer = state.server;
            state.callback = new Network.Callback(SetUpClient);
        }

        private void SetUpClient(Preserved_State_Object state)
        {
            lock(NamesSockets)
            {
                NamesSockets.Add(state.data, state.socket);
            }

            //For original UID's: have a counter that counts up and gives unique uid's
            //When a cube is removed, store the uid in a stack to be reused.
            //If there is nothing on the stack, increment the counter and use that UID.
            //IF there is something on the stack, pop it and use that as the uid.


            // Generate 2 random starting coords within our world, check if other players are there, then send if player won't get eaten immediately. (helper method)
            double x, y;
            FindStartingCoords(out x, out y);
            Cube cube = new Cube(x, y, GetUid(), false, state.data, World.PLAYER_START_MASS, GetColor(), 0);

            lock(World)
            {
                World.Cubes.Add(cube.uid, cube);
            }

            state.callback = new Network.Callback(ManageData);
            Network.Send(state.socket, JsonConvert.SerializeObject(cube) + "\n");

            //Testing code
            for(int i = 0; i < 500; i++)
                Network.Send(state.socket, JsonConvert.SerializeObject(new Cube(x, y, GetUid(), true, "", World.FOOD_MASS, GetColor(), 0)) + "\n");


            // Compute, create strings of all world data,
            //send all datat.

            //Network.Send(state.socket, )

            Network.I_Want_More_Data(state);
            //Flow: Get name, send cube, then send all world info, then start the flow back and forth as you receive and send information and requests.
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private void ManageData(Preserved_State_Object state)
        {
            DataReceived.Append(state.data);

            //Network.Send(state.socket, DataSent.ToString());
            double x, y;
            FindStartingCoords(out x, out y);
            Network.Send(state.socket, JsonConvert.SerializeObject(new Cube(x, y, GetUid(), true, "", World.FOOD_MASS, GetColor(), 0)));
        }


        /// <summary>
        /// Finds starting coordinates for a new player cube so that it isn't immediately consumed
        /// </summary>
        private void FindStartingCoords(out double x, out double y)
        {
            //Implement this
            x = RandomNumber.Next((int)World.PLAYER_START_WIDTH, World.WIDTH - (int)World.PLAYER_START_WIDTH);
            y = RandomNumber.Next((int)World.PLAYER_START_WIDTH, World.HEIGHT - (int)World.PLAYER_START_WIDTH);

            //More complicated stuff looking at other players and what not. Recursion?
            if (true)
                return;
            else
                FindStartingCoords(out x, out y);
        }


        /// <summary>
        /// Helper method: creates a unique uid to give a cube
        /// </summary>
        /// <returns></returns>
        private int GetUid()
        {
            return (Uids.Count > 0) ? Uids.Pop() : Uid++;
        }


        /// <summary>
        /// Gives the cube a color
        /// </summary>
        /// <returns></returns>
        private int GetColor()
        {
            return RandomNumber.Next(Int32.MinValue, Int32.MaxValue);
        }


        private void HeartBeatTick(object state)
        {
            string data;

            lock (World)
            {
                if (World.MAX_FOOD_COUNT > World.Food.Count)
                {
                    double x, y;
                    FindStartingCoords(out x, out y);
                    World.Food.Add(new Cube(x, y, GetUid(), true, "", World.FOOD_MASS, GetColor(), 0));
                }

                data = World.SerializeAllCubes();
                
            }

            lock(NamesSockets)
            {
                foreach (Socket s in NamesSockets.Values)
                {
                    Network.Send(s, data);
                }
            }


        }


    }
}
