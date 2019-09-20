using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Models;

namespace Deltin.Deltinteger.Pathfinder
{
    class PathMap
    {
        private static readonly Log Log = new Log("PathMap");

        private static readonly Variable IsBuilding = Variable.D;
        private static readonly Variable BuildingNodeIndex = Variable.E;
        private static readonly Variable BuildingNode = Variable.F;
        private static readonly Variable ConnectIndex = Variable.G;
        private static readonly Variable Connect1 = Variable.H;
        private static readonly Variable Connect2 = Variable.I;

        public static PathMap ImportFromCSV(string file)
        {
            CsvFrame[] frames = CsvFrame.ParseSet(File.ReadAllLines(file));

            for (int i = 0; i < frames.Length; i++)
                if (frames[i].VariableSetOwner != "Global")
                {
                    Log.Write(LogLevel.Normal, new ColorMod("Error: need the global variable set, got " + frames[i].VariableSetOwner + " instead on line " + i, ConsoleColor.Red));
                    Console.ReadLine();
                }

            bool started = false;
            int startAt = -1;
            for (int i = frames.Length - 1; i >= 0; i--)
            {
                if (frames[i].VariableValues[IsBuilding] is CsvBoolean && ((CsvBoolean)frames[i].VariableValues[IsBuilding]).Value)
                {
                    started = true;
                }
                else if (started)
                {
                    startAt = i + 1;
                    break;
                }
            }

            int last = -1;

            List<Vertex> vectors = new List<Vertex>();
            for (int i = startAt; i < frames.Length; i++)
                if (frames[i].VariableValues[BuildingNodeIndex].Changed && frames[i].VariableValues[BuildingNode] is CsvVector)
                {
                    vectors.Add(((CsvVector)frames[i].VariableValues[BuildingNode]).Value);
                    last = i + 1;
                }
            
            List<Segment> segments = new List<Segment>();
            for (int i = last; i < frames.Length; i++)
                if (frames[i].VariableValues[ConnectIndex].Changed && frames[i].VariableValues[Connect1] is CsvNumber && frames[i].VariableValues[Connect2] is CsvNumber)
                {
                    segments.Add(new Segment(
                        (int)((CsvNumber)frames[i].VariableValues[Connect1]).Value,
                        (int)((CsvNumber)frames[i].VariableValues[Connect2]).Value
                    ));
                }
            
            return new PathMap(vectors.ToArray(), segments.ToArray());
        }

        public Vertex[] Nodes { get; }
        public Segment[] Segments { get; }

        public PathMap(Vertex[] nodes, Segment[] segments)
        {
            Nodes = nodes;
            Segments = segments;
        }
    }

    class Segment
    {
        public int Node1 { get; }
        public int Node2 { get; }

        public Segment(int node1, int node2)
        {
            Node1 = node1;
            Node2 = node2;
        }
    }
}