using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;

namespace InstanceGenerator
{
    public class Coordinates
    {
        public int x = 0;
        public int y = 0;
    }

    public class InstGenConfig
    {
        public int n_depots { get; set; } = 10;
        public int n_nodes { get; set; } = 100;
        public int x_dim { get; set; } = 400;
        public int y_dim { get; set; } = 400;
        public int depot_creation_radius { get; set; } = 50;
        public Random RNG { get; set; } = new Random(1000);
    }

    public class InstanceGenerator
    {
        public Coordinates[] depot_node { get; set; }
        public Coordinates[] node { get; set; }

        private void initialize(InstGenConfig Config)
        {
            depot_node = new Coordinates[Config.n_depots];
            for (int i = 0; i < Config.n_depots; i++)
            {
                depot_node[i] = new Coordinates();
            }
            node = new Coordinates[Config.n_nodes];
            for (int i = 0; i < Config.n_nodes; i++)
            {
                node[i] = new Coordinates();
            }
        }

        private void create_data(InstGenConfig Config)
        {
            create_depots(Config);
            create_nodes(Config);
        }

        private void create_depots(InstGenConfig Config)
        {
            for (int i = 0; i < Config.n_depots; i++)
            {
                int x = Config.RNG.Next(0, Config.x_dim - Config.depot_creation_radius + 1);
                int y = Config.RNG.Next(0, Config.y_dim - Config.depot_creation_radius + 1);
                depot_node[i].x = x;
                depot_node[i].y = y;
            }
        }

        private void create_nodes(InstGenConfig Config)
        {
            for (int i = 0; i < Config.n_nodes; i++)
            {
                int x = Config.RNG.Next(0, Config.x_dim + 1);
                int y = Config.RNG.Next(0, Config.y_dim + 1);
                node[i].x = x;
                node[i].y = y;
            }
        }

        private void write_to_file(StreamWriter File)
        {
            // to be implemented
        }

        public void create_instance(InstGenConfig Config, String filename="")
        {
            initialize(Config);
            if (filename.Length == 0)
            {
                create_data(Config);
            }
            else
            {
                create_data(Config);
                write_to_file(new StreamWriter(filename));
            }
        }

        public void draw_instance(InstGenConfig Config, String filename)
        {
            // to do
            Point[] Depots = new Point[Config.n_depots];
            for (int i = 0; i < Config.n_depots; i++)
            {
                Depots[i] = new Point(depot_node[i].x, depot_node[i].y);
            }
            Point[] Nodes = new Point[Config.n_nodes];
            for (int i = 0; i < Config.n_nodes; i++)
            {
                Nodes[i] = new Point(node[i].x, node[i].y);
            }
        }
    }

    class main
    {
        static void Main(string[] args)
        {
            InstanceGenerator IG = new InstanceGenerator();
            IG.create_instance(new InstGenConfig());
            return;
        }
    }
}
