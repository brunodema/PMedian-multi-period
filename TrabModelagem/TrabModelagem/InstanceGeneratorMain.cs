using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace InstanceGenerator
{
    public class Coordinates
    {
        public int x = 0;
        public int y = 0;
        public PriorityGroup group = PriorityGroup.HIGH;
    }

    public class CONSTANTS
    {
        public const int TIME_PERIODS = 5;
        public const int MAX_DEPOTS_NODES_PER_PERIOD = 10;
        public const int MAX_NODES_PER_DEPOT = 50;
        public const double DEPOT_USAGE_COST = 1000;
        public const int N_DEPOTS = 5;
        public const int DEPOT_CREATION_RADIUS = 100;
        public const int N_NODES = 500;
        public const int NUMBER_OF_PRIORITY_GROUPS = 3;
        public const int X_DIM = 1000;
        public const int Y_DIM = 1000;
        public const int RNG_SEED = 1000;
    }

    public enum PriorityGroup
    {
        HIGH,
        MEDIUM,
        LOW,
        NONE
    }

    public class InstanceGeneratorConfig
    {
        public int time_periods { get; set; } = CONSTANTS.TIME_PERIODS;
        public int number_of_priority_groups { get; set; } = CONSTANTS.NUMBER_OF_PRIORITY_GROUPS;
        public int max_depot_nodes_per_period { get; set; } = CONSTANTS.MAX_DEPOTS_NODES_PER_PERIOD;
        public int max_nodes_per_depot { get; set; } = CONSTANTS.MAX_NODES_PER_DEPOT;
        public double depot_usage_cost { get; set; } = CONSTANTS.DEPOT_USAGE_COST;
        public int n_depots { get; set; } = CONSTANTS.N_DEPOTS;
        public int n_nodes { get; set; } = CONSTANTS.N_NODES;
        public int x_dim { get; set; } = CONSTANTS.X_DIM;
        public int y_dim { get; set; } = CONSTANTS.Y_DIM;
        public int depot_creation_radius { get; set; } = CONSTANTS.DEPOT_CREATION_RADIUS;
        public Random RNG { get; set; } = new Random(CONSTANTS.RNG_SEED);

        public PriorityGroup getPriorityGroup(int number_of_groups)
        {
            if (number_of_groups <= 1)
            {
                return PriorityGroup.HIGH;
            }
            else
            {
                int dummy = number_of_groups > 3 ? RNG.Next(number_of_groups + 1) : RNG.Next(3);
                return (PriorityGroup)dummy;
            }
        }
    }

    public class InstanceGeneratorMain
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

        public InstanceGeneratorMain(InstanceGeneratorConfig pinstanceGeneratorConfig)
        {
            instanceGeneratorConfig = pinstanceGeneratorConfig;
        }

        public InstanceGeneratorConfig getInstanceGeneratorConfig()
        {
            return instanceGeneratorConfig;
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
                depot_node[i].x = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.x_dim - instanceGeneratorConfig.depot_creation_radius + 1);
                depot_node[i].y = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.y_dim - instanceGeneratorConfig.depot_creation_radius + 1);
                depot_node[i].group = PriorityGroup.NONE;
            }
        }

        private void create_nodes()
        {
            for (int i = 0; i < instanceGeneratorConfig.n_nodes; i++)
            {
                customer_node[i].x = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.x_dim + 1);
                customer_node[i].y = instanceGeneratorConfig.RNG.Next(0, instanceGeneratorConfig.y_dim + 1);
                customer_node[i].group = instanceGeneratorConfig.getPriorityGroup(instanceGeneratorConfig.number_of_priority_groups);
            }
        }

        private void calculate_customer_depot_assignment_cost()
        {
            for (int i = 0; i < instanceGeneratorConfig.n_nodes; i++)
            {
                for (int j = 0; j < instanceGeneratorConfig.n_depots; j++)
                {
                    double cost = Math.Sqrt(Math.Pow((customer_node[i].x - depot_node[j].x), 2) + Math.Pow((customer_node[i].y - depot_node[j].y), 2));
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

        public void create_instance(String filename = "")
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
            InstanceDrawing instanceDrawing = new InstanceDrawing(this, new DrawingSettings());
            instanceDrawing.draw(filename);
        }
    }

    public enum OBJECT_COLOR
    {
        NEUTRAL_BLACK,
        DEPOT_NODE,
        CUSTOMER_NODE,
        ARROW_LINK
    }

    public static class ColorProgression
    {
        private static Dictionary<OBJECT_COLOR, Color> CurrentColor = new Dictionary<OBJECT_COLOR, Color>
        {
            { OBJECT_COLOR.NEUTRAL_BLACK, Color.Black},
            { OBJECT_COLOR.DEPOT_NODE, Color.Black},
            { OBJECT_COLOR.CUSTOMER_NODE, Color.Red},
            { OBJECT_COLOR.ARROW_LINK, Color.LightBlue }
        };

        public static Color getColor(OBJECT_COLOR object_type)
        {
            return CurrentColor[object_type];
        }
        public static Color? getColor(PriorityGroup group)
        {
            switch(group)
            {
                case PriorityGroup.HIGH:
                    return Color.Red;
                case PriorityGroup.MEDIUM:
                    return Color.Orange;
                case PriorityGroup.LOW:
                    return Color.Yellow;
                case PriorityGroup.NONE:
                    return Color.Black;
            }
            return null;
        }
    }

    public class DrawingSettings
    {
        public int point_radius { get; set; } = 3;
        public double board_radius_factor { get; set; } = 1.1;
    }

    public class InstanceDrawing
    {
        private class RectangleDrawing
        {
            public RectangleDrawing(Rectangle p_rectangle, PriorityGroup p_group)
            {
                Rectangle = p_rectangle;
                PriorityGroup = p_group;
            }

            public Rectangle Rectangle;
            public PriorityGroup PriorityGroup;
        }

        private readonly InstanceGeneratorMain instanceGenerator;
        private readonly DrawingSettings drawingSettings;

        private RectangleDrawing[] depot;
        private RectangleDrawing[] node;
        private Image image;
        private Graphics graphics;
        private Brush brush;
        private Pen pen;
        private Rectangle board;

        public InstanceDrawing(InstanceGeneratorMain pinstanceGenerator, DrawingSettings pdrawingSettings)
        {
            this.instanceGenerator = pinstanceGenerator;
            this.drawingSettings = pdrawingSettings;

            this.image = new Bitmap((int)(instanceGenerator.getInstanceConfig().x_dim * drawingSettings.board_radius_factor), (int)(instanceGenerator.getInstanceConfig().y_dim * drawingSettings.board_radius_factor));
            Color color = ColorProgression.getColor(OBJECT_COLOR.NEUTRAL_BLACK);
            this.pen = new Pen(color);
            this.brush = new SolidBrush(color);
            this.graphics = Graphics.FromImage(this.image);
        }

        public Image getImage()
        {
            return this.image;
        }

        public DrawingSettings getDrawingSettings()
        {
            return this.drawingSettings;
        }

        public void draw(string filename)
        {
            this.initialize_node_drawings();
            this.draw_board();
            this.draw_nodes();

            this.image.Save(filename + ".bmp");
        }

        private void initialize_node_drawings()
        {
            depot = new RectangleDrawing[instanceGenerator.getInstanceConfig().n_depots];
            for (int i = 0; i < instanceGenerator.getInstanceConfig().n_depots; i++)
            {
                depot[i] = new RectangleDrawing(new Rectangle(instanceGenerator.depot_node[i].x - drawingSettings.point_radius / 2, instanceGenerator.depot_node[i].y - drawingSettings.point_radius / 2, drawingSettings.point_radius, drawingSettings.point_radius), PriorityGroup.HIGH);
            }
            node = new RectangleDrawing[instanceGenerator.getInstanceConfig().n_nodes];
            for (int i = 0; i < instanceGenerator.getInstanceConfig().n_nodes; i++)
            {
                node[i] = new RectangleDrawing(new Rectangle(instanceGenerator.customer_node[i].x, instanceGenerator.customer_node[i].y, drawingSettings.point_radius, drawingSettings.point_radius), instanceGenerator.customer_node[i].group);
            }
        }

        private void draw_board()
        {
            board = new Rectangle(0, 0, (int)(instanceGenerator.getInstanceConfig().x_dim * (drawingSettings.board_radius_factor - 0.05)), (int)(instanceGenerator.getInstanceConfig().y_dim * (drawingSettings.board_radius_factor - 0.05)));
            this.graphics.DrawRectangle(this.pen, board);
        }

        private void draw_nodes()
        {
            foreach (var dnode in depot)
            {
                this.graphics.FillRectangle(brush, dnode.Rectangle);
            }
            foreach (var nnode in node)
            {
                this.graphics.FillRectangle(new SolidBrush((Color)ColorProgression.getColor(nnode.PriorityGroup)), nnode.Rectangle);
            }
        }
    }

    class main
    {
        static void Main(string[] args)
        {
            InstanceGeneratorConfig instanceGeneratorConfig = new InstanceGeneratorConfig();
            InstanceGeneratorMain IG = new InstanceGeneratorMain(instanceGeneratorConfig);
            IG.create_instance();
            InstanceDrawing instanceDrawing = new InstanceDrawing(IG, new DrawingSettings());
            instanceDrawing.draw("instance.bmp");
            return;
        }
    }
}
