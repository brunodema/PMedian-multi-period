using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Dynamic;

namespace InstanceGenerator
{
    public class Coordinates
    {
        public int x = 0;
        public int y = 0;
    }

    public class InstanceGeneratorConfig
    {
        public int time_periods { get; set; } = 5;
        public int max_depot_nodes_per_period { get; set; } = 3;
        public int max_nodes_per_depot { get; set; } = 100;
        public double depot_usage_cost { get; set; } = 500;
        public int n_depots { get; set; } = 10;
        public int n_nodes { get; set; } = 100;
        public int x_dim { get; set; } = 400;
        public int y_dim { get; set; } = 400;
        public int depot_creation_radius { get; set; } = 50;
        public Random RNG { get; set; } = new Random(1000);
    }

    public class InstanceGenerator
    {
        private readonly InstanceGeneratorConfig instanceGeneratorConfig;

        public InstanceGeneratorConfig getInstanceConfig()
        {
            return instanceGeneratorConfig;
        }

        public Coordinates[] depot_node { get; set; }
        public Coordinates[] customer_node { get; set; }

        public double[,] customer_depot_assignment_cost { get; set; }
        public double[] depot_usage_cost { get; set; }

        public InstanceGenerator(InstanceGeneratorConfig pinstanceGeneratorConfig)
        {
            instanceGeneratorConfig = pinstanceGeneratorConfig;
        }

        private void initialize()
        {
            depot_node = new Coordinates[instanceGeneratorConfig.n_depots];
            for (int i = 0; i < instanceGeneratorConfig.n_depots; i++)
            {
                depot_node[i] = new Coordinates();
            }
            customer_node = new Coordinates[instanceGeneratorConfig.n_nodes];
            for (int i = 0; i < instanceGeneratorConfig.n_nodes; i++)
            {
                customer_node[i] = new Coordinates();
            }
            customer_depot_assignment_cost = new double[instanceGeneratorConfig.n_nodes, instanceGeneratorConfig.n_depots];
            depot_usage_cost = new double[instanceGeneratorConfig.n_depots];
        }

        private void set_depot_usage_cost()
        {
            for (int j = 0; j < this.depot_node.Length; j++)
            {
                depot_usage_cost[j] = instanceGeneratorConfig.depot_usage_cost;
            }
        }

        private void create_data()
        {
            create_depots();
            create_nodes();
            set_depot_usage_cost();
            calculate_customer_depot_assignment_cost();
        }

        private void create_depots()
        {
            for (int i = 0; i < instanceGeneratorConfig.n_depots; i++)
            {
                int x = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.x_dim - instanceGeneratorConfig.depot_creation_radius + 1);
                int y = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.y_dim - instanceGeneratorConfig.depot_creation_radius + 1);
                depot_node[i].x = x;
                depot_node[i].y = y;
            }
        }

        private void create_nodes()
        {
            for (int i = 0; i < instanceGeneratorConfig.n_nodes; i++)
            {
                int x = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.x_dim + 1);
                int y = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.y_dim + 1);
                customer_node[i].x = x;
                customer_node[i].y = y;
            }
        }

        private void calculate_customer_depot_assignment_cost()
        {
            for (int i = 0; i < instanceGeneratorConfig.n_nodes; i++)
            {
                for (int j = 0; j < instanceGeneratorConfig.n_depots; j++)
                {
                    double cost = Math.Sqrt(Math.Pow((customer_node[i].x - depot_node[j].x), 2) + Math.Pow((customer_node[i].y - depot_node[j].y),2 ));
                    customer_depot_assignment_cost[i, j] = cost;
                }
            }
        }

        private void calculate_depot_usage_cost(double usage_cost = 100.00)
        {
            for (int i = 0; i < depot_node.Length; i++)
            {
                depot_usage_cost[i] = usage_cost;
            }
        }

        private void write_to_file(StreamWriter File)
        {
            // to be implemented
        }

        public void create_instance(String filename="")
        {
            initialize();
            create_data();
            if (filename.Length == 0)
            {
                //to do
            }
            else
            {
                write_to_file(new StreamWriter(filename));
            }
        }

        public void draw_instance(String filename)
        {
            // to do
            Point[] Depots = new Point[instanceGeneratorConfig.n_depots];
            for (int i = 0; i < instanceGeneratorConfig.n_depots; i++)
            {
                Depots[i] = new Point(depot_node[i].x, depot_node[i].y);
            }
            Point[] Nodes = new Point[instanceGeneratorConfig.n_nodes];
            for (int i = 0; i < instanceGeneratorConfig.n_nodes; i++)
            {
                Nodes[i] = new Point(customer_node[i].x, customer_node[i].y);
            }
        }
    }

    class main
    {
        static void Main(string[] args)
        {
            InstanceGeneratorConfig instanceGeneratorConfig = new InstanceGeneratorConfig();
            InstanceGenerator IG = new InstanceGenerator(instanceGeneratorConfig);
            IG.create_instance();
            return;
        }
    }
}
