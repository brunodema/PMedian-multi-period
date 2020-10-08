using Gurobi;
using InstanceGenerator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PMedians
{
    public class VariableGenerator
    {
        private readonly InstanceGenerator.InstanceGeneratorMain InstData;
        private Gurobi.GRBModel Model;
        public GRBVar[,] depot_usage;
        public GRBVar[,,] customer_depot_designation;
        public GRBVar[,] group_is_serviced;

        public VariableGenerator(InstanceGenerator.InstanceGeneratorMain pInstData, Gurobi.GRBModel pModel)
        {
            InstData = pInstData;
            Model = pModel;
            depot_usage = new GRBVar[InstData.getInstanceConfig().n_depots, InstData.getInstanceConfig().time_periods];
            customer_depot_designation = new GRBVar[InstData.getInstanceConfig().n_nodes, InstData.getInstanceConfig().n_depots, InstData.getInstanceConfig().time_periods];
            group_is_serviced = new GRBVar[InstData.getInstanceConfig().number_of_priority_groups, InstData.getInstanceConfig().time_periods];
        }

        private void create_depot_usage_vars()
        {
            for (int j = 0; j < InstData.getInstanceConfig().n_depots; j++)
            {
                for (int t = 0; t < InstData.getInstanceConfig().time_periods; t++)
                {
                    this.depot_usage[j, t] = Model.AddVar(0.00, 1.00, 1.00, GRB.BINARY, String.Format("y_j{0}_t{1}", j, t));
                }
            }
            return;
        }
        private void create_depot_customer_designation_vars()
        {
            for (int j = 0; j < InstData.getInstanceConfig().n_depots; j++)
            {
                for (int i = 0; i < InstData.getInstanceConfig().n_nodes; i++)
                {
                    for (int t = 0; t < InstData.getInstanceConfig().time_periods; t++)
                    {
                        this.customer_depot_designation[i, j, t] = Model.AddVar(0.00, 1.00, 1.00, GRB.BINARY, String.Format("x_i{0}_j{1}_t{2}", i, j, t));
                    }
                }
            }
            return;
        }
        private void create_group_is_serviced_vars()
        {
            for (int prior = 0; prior < InstData.getInstanceGeneratorConfig().number_of_priority_groups; prior++)
            {
                for (int t = 0; t < InstData.getInstanceGeneratorConfig().time_periods; t++)
                {
                    this.group_is_serviced[prior, t] = Model.AddVar(0.00, 1.00, 1.00, GRB.BINARY, String.Format($"G_g{prior}_{t}", prior));
                }
            }
        }
        public void make_all_vars()
        {
            this.create_depot_usage_vars();
            this.create_depot_customer_designation_vars();
            this.create_group_is_serviced_vars();
        }
    }

    public class ConstraintGenerator
    {
        private GRBModel Model;
        private readonly VariableGenerator variableGenerator;
        private readonly InstanceGeneratorMain instanceGenerator;

        public ConstraintGenerator(GRBModel pModel, VariableGenerator pvariableGenerator, InstanceGeneratorMain pinstanceGenerator)
        {
            Model = pModel;
            variableGenerator = pvariableGenerator;
            instanceGenerator = pinstanceGenerator;
        }

        public void max_depot_nodes_per_period(int n_DepotNodes)
        {
            for (int t = 0; t < instanceGenerator.getInstanceConfig().time_periods; t++)
            {
                GRBLinExpr sum = 0;
                for (int j = 0; j < instanceGenerator.getInstanceConfig().n_depots; j++)
                {
                    sum += variableGenerator.depot_usage[j, t];
                }
                Model.AddConstr(sum <= instanceGenerator.getInstanceConfig().max_depot_nodes_per_period, String.Format("max_depot_nodes_per_period_t{0}", t));
            }
        }
        public void max_nodes_per_depot(int nodelimit)
        {
            for (int t = 0; t < instanceGenerator.getInstanceConfig().time_periods; t++)
            {
                for (int j = 0; j < instanceGenerator.getInstanceConfig().n_depots; j++)
                {
                    GRBLinExpr sum = 0;
                    for (int i = 0; i < instanceGenerator.getInstanceConfig().n_nodes; i++)
                    {
                        sum += variableGenerator.customer_depot_designation[i, j, t];
                    }
                    Model.AddConstr(sum <= instanceGenerator.getInstanceConfig().max_nodes_per_depot, String.Format("max_nodes_per_depot_j{0}_t{1}", j, t));
                }
            }
        }
        public void one_visit_per_node()
        {
            for (int i = 0; i < instanceGenerator.getInstanceConfig().n_nodes; i++)
            {
                GRBLinExpr sum = 0;
                for (int t = 0; t < instanceGenerator.getInstanceConfig().time_periods; t++)
                {
                    for (int j = 0; j < instanceGenerator.getInstanceConfig().n_depots; j++)
                    {
                        sum += variableGenerator.customer_depot_designation[i, j, t];
                    }
                }
                Model.AddConstr(sum == 1, String.Format("one_visit_per_node_i{0}", i));
            }
        }
        public void service_only_by_active_depot()
        {
            for (int t = 0; t < instanceGenerator.getInstanceConfig().time_periods; t++)
            {
                for (int j = 0; j < instanceGenerator.getInstanceConfig().n_depots; j++)
                {
                    GRBLinExpr sum = 0;
                    for (int i = 0; i < instanceGenerator.getInstanceConfig().n_nodes; i++)
                    {
                        sum += variableGenerator.customer_depot_designation[i, j, t];
                    }
                    Model.AddConstr(sum <= variableGenerator.depot_usage[j, t] * instanceGenerator.getInstanceConfig().max_nodes_per_depot, String.Format("service_only_by_active_depot_j{0}_t{1}", j, t));
                }
            }
        }
        public void all_group_nodes_visited_in_period()
        {
            int num_prior = instanceGenerator.getInstanceConfig().number_of_priority_groups;
            if (num_prior > 0)
            {
                for (int prior = 0; prior < instanceGenerator.getInstanceConfig().number_of_priority_groups; prior++)
                {
                    for (int t = 0; t < instanceGenerator.getInstanceConfig().time_periods; t++)
                    {
                        int count = instanceGenerator.customer_node.Where(x => x.group == (PriorityGroup)prior).Count();
                        GRBLinExpr sum1 = 0;
                        for (int taux = 0; taux <= t; taux++)
                        {
                            for (int i = 0; i < instanceGenerator.getInstanceConfig().n_nodes; i++)
                            {
                                if (instanceGenerator.customer_node[i].group == (PriorityGroup)prior)
                                {
                                    for (int j = 0; j < instanceGenerator.getInstanceConfig().n_depots; j++)
                                    {
                                        sum1 += variableGenerator.customer_depot_designation[i, j, taux];
                                    }
                                }
                            }
                        }
                        Model.AddConstr(sum1 == variableGenerator.group_is_serviced[prior, t] * count, $"group_is_serviced_prior{prior}_{t}");
                    }
                }
            }
            else
            {
                return;
            }
        }
        public void group_precedence()
        {
            int num_prior = instanceGenerator.getInstanceConfig().number_of_priority_groups;
            if (num_prior > 0)
            {
                for (int i = 0; i < instanceGenerator.getInstanceConfig().n_nodes; i++)
                {
                    for (int prior = 1; prior < instanceGenerator.getInstanceConfig().number_of_priority_groups; prior++)
                    {
                        if (instanceGenerator.customer_node[i].group == (PriorityGroup)prior)
                        {
                            for (int t = 0; t < instanceGenerator.getInstanceConfig().time_periods; t++)
                            {
                                for (int j = 0; j < instanceGenerator.getInstanceConfig().n_depots; j++)
                                {
                                    Model.AddConstr(variableGenerator.customer_depot_designation[i, j, t] <= variableGenerator.group_is_serviced[prior - 1, t], "dadsdff"); // talvez tenha que incorporar o somatorio cumulativo aqui
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return;
            }
        }

        public void make_all_constraints()
        {
            this.max_depot_nodes_per_period(instanceGenerator.getInstanceConfig().max_depot_nodes_per_period);
            this.one_visit_per_node();
            this.service_only_by_active_depot();
            this.all_group_nodes_visited_in_period();
            this.group_precedence();
            this.setup_objective();
        }

        public void setup_objective()
        {
            GRBLinExpr sum_depot_expr = 0;
            GRBLinExpr sum_node_expr = 0;
            for (int t = 0; t < instanceGenerator.getInstanceConfig().time_periods; t++)
            {
                for (int j = 0; j < instanceGenerator.getInstanceConfig().n_depots; j++)
                {
                    sum_depot_expr += variableGenerator.depot_usage[j, t] * instanceGenerator.getInstanceConfig().depot_usage_cost;
                    for (int i = 0; i < instanceGenerator.getInstanceConfig().n_nodes; i++)
                    {
                        sum_node_expr += variableGenerator.customer_depot_designation[i, j, t] * instanceGenerator.customer_depot_assignment_cost[i, j];
                    }
                }
            }
            Model.SetObjective(sum_depot_expr + sum_node_expr, GRB.MINIMIZE);
        }
    }

    class PMedianConfig
    {
        // to do
    }

    public class PMedian : GRBModel
    {
        private readonly InstanceGenerator.InstanceGeneratorMain Instance;
        private VariableGenerator variableGenerator;
        private ConstraintGenerator constraintGenerator;
        private string filename;

        public PMedian(InstanceGenerator.InstanceGeneratorMain pInstance, string pfilename = "PMEDIAN", string presults_filename = null)
            : base(new GRBEnv(pfilename + ".log"))
        {
            Instance = pInstance;
            filename = pfilename;
        }

        public InstanceGenerator.InstanceGeneratorMain getInstanceGenerator()
        {
            return this.Instance;
        }

        public VariableGenerator getVariableGenerator()
        {
            return this.variableGenerator;
        }

        public void setup_problem()
        {
            variableGenerator = new VariableGenerator(Instance, this);
            variableGenerator.make_all_vars();
            constraintGenerator = new ConstraintGenerator(this, variableGenerator, Instance);
            constraintGenerator.make_all_constraints();
        }

        public void draw_instance()
        {
            // to do
        }

        public int solve_instance(double runtime)
        {
            this.Set(GRB.DoubleParam.TimeLimit, runtime);
            this.Optimize();
            if (this.Status == GRB.Status.INFEASIBLE)
            {
                this.IIS();
            }
            return 0;
        }

        private void IIS()
        {
            this.ComputeIIS();
            this.Write(filename + ".ilp");
        }

        public void write_lp()
        {
            this.Write(filename + ".lp");
        }

        public void write_sol()
        {
            this.Write(filename + ".sol");
        }

        public void publish_model()
        {
            this.write_lp();
            this.write_sol();
        }

        public void draw_solution(InstanceDrawing instanceDrawing, string filename_template)
        {
            SolutionDrawing solutionDrawing = new SolutionDrawing(instanceDrawing, this);
            solutionDrawing.draw(filename_template);
        }

        public void write_results_to_file(string results_filename)
        {
            if (File.Exists(results_filename))
            {
                using (StreamWriter BufferedStream = File.AppendText(results_filename))
                {
                    BufferedStream.WriteLine($"{this.ObjVal},{this.ObjBound},{this.MIPGap},{this.Runtime},{this.NodeCount},{this.NumVars},{this.NumConstrs}");
                    BufferedStream.Close();
                }
            }
            else
            {
                using (StreamWriter BufferedStream = new StreamWriter(results_filename))
                {
                    BufferedStream.Write($"{this.ObjVal},{this.ObjBound},{this.MIPGap},{this.Runtime},{this.NodeCount},{this.NumVars},{this.NumConstrs}\n");
                    BufferedStream.Close();
                }
            }
        }
    }

    public class SolutionDrawing
    {
        private readonly InstanceDrawing instanceDrawing;
        private readonly PMedian pMedian;

        private Pen pen;

        private Graphics[] graphics;
        private Image[] image;

        public SolutionDrawing(InstanceDrawing pinstanceDrawing, PMedian pPMedian)
        {
            instanceDrawing = pinstanceDrawing;
            pMedian = pPMedian;

            pen = new Pen(ColorProgression.getColor(OBJECT_COLOR.ARROW_LINK));
            this.graphics = new Graphics[this.pMedian.getInstanceGenerator().getInstanceConfig().time_periods];
            this.image = new Image[this.pMedian.getInstanceGenerator().getInstanceConfig().time_periods];

            for (int t = 0; t < pMedian.getInstanceGenerator().getInstanceConfig().time_periods; t++)
            {
                //this.image[t] = new Bitmap((int)(pMedian.getInstanceGenerator().getInstanceConfig().x_dim * instanceDrawing.getDrawingSettings().board_radius_factor), (int)(pMedian.getInstanceGenerator().getInstanceConfig().y_dim * instanceDrawing.getDrawingSettings().board_radius_factor));
                this.image[t] = (Image)this.instanceDrawing.getImage().Clone();
                this.graphics[t] = Graphics.FromImage(this.image[t]);
            }
        }

        public void draw(string filename_template)
        {
            this.draw_node_designations();
            this.draw_solution_all_periods(filename_template);
        }

        private void draw_solution_all_periods(string filename_template)
        {
            for (int t = 0; t < this.pMedian.getInstanceGenerator().getInstanceConfig().time_periods; t++)
            {
                this.image[t].Save(filename_template + "_" + t + ".bmp");
            }
        }

        private void draw_instance(string filename)
        {
            this.instanceDrawing.draw(filename);
        }

        private void draw_node_designations()
        {
            for (int t = 0; t < pMedian.getInstanceGenerator().getInstanceConfig().time_periods; t++)
            {
                for (int j = 0; j < pMedian.getInstanceGenerator().getInstanceConfig().n_depots; j++)
                {
                    Point dnode_point = new Point(pMedian.getInstanceGenerator().depot_node[j].x, pMedian.getInstanceGenerator().depot_node[j].y);
                    for (int i = 0; i < pMedian.getInstanceGenerator().getInstanceConfig().n_nodes; i++)
                    {
                        if (pMedian.getVariableGenerator().customer_depot_designation[i, j, t].X > 0.5)
                        {
                            this.graphics[t].DrawLine(this.pen, new Point(pMedian.getInstanceGenerator().customer_node[i].x, pMedian.getInstanceGenerator().customer_node[i].y), dnode_point);
                            this.graphics[t].DrawString(pMedian.getInstanceGenerator().customer_node[i].group.ToString()[0].ToString(), new Font(FontFamily.GenericSansSerif, 10), new SolidBrush((Color)ColorProgression.getColor(pMedian.getInstanceGenerator().customer_node[i].group)), pMedian.getInstanceGenerator().customer_node[i].x, pMedian.getInstanceGenerator().customer_node[i].y);
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Class to clump auxiliary methods for the P-Medians model execution.
    /// </summary>
    public static class PMedianController
    {
        /// <summary>
        /// Delete files with the given terminations.
        /// </summary>
        /// <param name="args">
        /// List of strings containing the terminations. Example: "'.bmp','.lp','.sol'"
        /// </param>
        public static void folder_cleanup(params string[] args)
        {
            foreach (var arg in args)
            {
                foreach (string file in Directory.GetFiles(".", "*" + arg))
                {
                    File.Delete(file);
                }
            }
        }
    }

    public static class ArgParser
    {
        private static string[] args;
        private static int curr_pos = 0;
        private static InstanceGeneratorConfig instanceGeneratorConfig;
        private static string drawing_path;
        private static double model_runtime = Double.MaxValue;
        public static string results_filename { private set; get; }

        public static InstanceGeneratorConfig getInstanceGeneratorConfig()
        {
            if (instanceGeneratorConfig != null)
            {
                return instanceGeneratorConfig;
            }
            else
            {
                throw new NullReferenceException();
            }
        }

        public static void getHelp()
        {
            help();
        }

        public static string getDrawingPath()
        {
            return drawing_path;
        }

        public static double getModelRuntime()
        {
            return model_runtime;
        }

        public static bool parse_args(string[] p_args)
        {
            args = p_args;
            bool is_config_set = ArgParser.parse_instance_config();
            ArgParser.parse_optional_flags(is_config_set);
            ArgParser.parse_help();
            if (curr_pos != args.Length)
            {
                end(1, $"Error when parsing arguments: incorrect syntax for '{string.Join(" ", args.ToArray())}'. Please refer to the usage instructions (-help).");
            }
            return true;
        }

        private static bool parse_instance_config()
        {
            switch (args[curr_pos])
            {
                case "-file":
                case "-f":
                    instanceGeneratorConfig = instance_from_file();
                    ++curr_pos;
                    return true;

                case "-val":
                case "-values":
                    instanceGeneratorConfig = instance_from_args();
                    ++curr_pos;
                    return true;

                case "-default":
                case "-dflt":
                    instanceGeneratorConfig = instance_from_default();
                    ++curr_pos;
                    return true;
            }
            return false;
        }

        private static void parse_optional_flags(bool is_config_set)
        {
            while (true)
            {
                if (curr_pos >= args.Length)
                {
                    return;
                }
                switch (args[curr_pos])
                {
                    case "-rng":
                    case "-randomseed":
                        if (is_config_set)
                        {
                            set_rng();
                            ++curr_pos;
                        }
                        else
                        {
                            end(1, "Error when parsing optional arguments: no instance configuration detected. Please refer to the usage instructions (-help).");
                        }
                        break;

                    case "-draw":
                        if (is_config_set)
                        {
                            set_drawing_options();
                            ++curr_pos;
                        }
                        else
                        {
                            end(1, "Error when parsing optional arguments: no instance configuration detected. Please refer to the usage instructions (-help).");
                        }
                        break;

                    case "-runtime":
                        if (is_config_set)
                        {
                            set_model_runtime();
                            ++curr_pos;
                        }
                        else
                        {
                            end(1, "Error when parsing optional arguments: no instance configuration detected. Please refer to the usage instructions (-help).");
                        }
                        break;

                    case "-o":
                    case "-output":
                        if (is_config_set)
                        {
                            results_filename = args[++curr_pos];
                            ++curr_pos;
                        }
                        else
                        {
                            end(1, "Error when parsing optional arguments: no instance configuration detected. Please refer to the usage instructions (-help)."); // all this repetition is atrocious
                        }
                        break;

                    default:
                        return;
                }
            }
        }

        private static void parse_help()
        {
            if (curr_pos >= args.Length)
            {
                return;
            }
            switch (args[curr_pos])
            {
                case "-?":
                case "-h":
                case "-help":
                    help();
                    ++curr_pos;
                    return;
            }
            return;
        }

        // ACTIONS
        private static InstanceGeneratorConfig instance_from_file()
        {
            ++curr_pos;
            instanceGeneratorConfig = new InstanceGeneratorConfig();
            List<string> values = new List<string>();
            try
            {
                using (StreamReader stream = new StreamReader(args[curr_pos]))
                {
                    while (stream.Peek() != -1)
                    {
                        values.Add(stream.ReadLine());
                    }
                }

                if (values.Count != 11)
                {
                    throw new Exception($"Number of detected parameters ({values.Count}) different than the expected value (11).");
                }

                Console.Write("Manual instance parameters input (-file) detected. Starting instance with:\n" +
        $"time periods = {values[0]}\n" +
        $"max operating depots per periods = {values[1]}\n" +
        $"max customer nodes per depot node = {values[2]}\n" +
        $"depot usage cost = {values[3]}\n" +
        $"number of depots = {values[4]}\n" +
        $"number of customers = {values[5]}\n" +
        $"depot creation radius = {values[6]}\n" +
        $"number of priority groups = {values[7]}\n" +
        $"x dimension of the board (used for visual representation) = {values[8]}\n" +
        $"y dimension of the board (used for visual representation) = {values[9]}\n");
                Console.WriteLine($"RNG seed = {values[10]}");

                return new InstanceGeneratorConfig()
                {
                    time_periods = Convert.ToInt32(values[0]),
                    max_depot_nodes_per_period = Convert.ToInt32(values[1]),
                    max_nodes_per_depot = Convert.ToInt32(values[2]),
                    depot_usage_cost = Convert.ToDouble(values[3]),
                    n_depots = Convert.ToInt32(values[4]),
                    n_nodes = Convert.ToInt32(values[5]),
                    depot_creation_radius = Convert.ToInt32(values[6]),
                    number_of_priority_groups = Convert.ToInt32(values[7]),
                    x_dim = Convert.ToInt32(values[8]),
                    y_dim = Convert.ToInt32(values[9]),
                    RNG = new Random(Convert.ToInt32(values[10]))
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                end(1, "Error when parsing instance parameters from file.");
                return null;
            }
        }

        private static InstanceGeneratorConfig instance_from_args()
        {
            ++curr_pos;
            List<string> values = new List<string>();
            try
            {
                while (curr_pos < args.Length)
                {
                    if (!args[curr_pos].StartsWith("-"))
                    {
                        values.Add(args[curr_pos]);
                        ++curr_pos;
                    }
                    else
                    {
                        break;
                    }
                }
                --curr_pos;
                if (values.Count != 10)
                {
                    end(1, String.Format("Error when parsing values: 10 values expected, only {0} detected.", values.Count));
                }

                Console.Write("Manual instance parameters input (-val) detected. Starting instance with:\n" +
                    $"time periods = {values[0]}\n" +
                    $"max operating depots per periods = {values[1]}\n" +
                    $"max customer nodes per depot node = {values[2]}\n" +
                    $"depot usage cost = {values[3]}\n" +
                    $"number of depots = {values[4]}\n" +
                    $"number of customers = {values[5]}\n" +
                    $"depot creation radius = {values[6]}\n" +
                    $"number of priority groups = {values[7]}\n" +
                    $"x dimension of the board (used for visual representation) = {values[8]}\n" +
                    $"y dimension of the board (used for visual representation) = {values[9]}\n");
                Console.WriteLine("RNG seed = 1000");

                return new InstanceGeneratorConfig()
                {
                    time_periods = Convert.ToInt32(values[0]),
                    max_depot_nodes_per_period = Convert.ToInt32(values[1]),
                    max_nodes_per_depot = Convert.ToInt32(values[2]),
                    depot_usage_cost = Convert.ToDouble(values[3]),
                    n_depots = Convert.ToInt32(values[4]),
                    n_nodes = Convert.ToInt32(values[5]),
                    depot_creation_radius = Convert.ToInt32(values[6]),
                    number_of_priority_groups = Convert.ToInt32(values[7]),
                    x_dim = Convert.ToInt32(values[8]),
                    y_dim = Convert.ToInt32(values[9]),
                    RNG = new Random(1000)
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                end(1, "Error when converting instance parameter value.");
                return null;
            }
        }

        private static InstanceGeneratorConfig instance_from_default()
        {
            Console.Write("Default instance parameters request (-dflt) detected. Starting instance with:\n" +
    $"time periods = {CONSTANTS.TIME_PERIODS}\n" +
    $"max operating depots per periods = {CONSTANTS.MAX_DEPOTS_NODES_PER_PERIOD}\n" +
    $"max customer nodes per depot node = {CONSTANTS.MAX_NODES_PER_DEPOT}\n" +
    $"depot usage cost = {CONSTANTS.DEPOT_USAGE_COST}\n" +
    $"number of depots = {CONSTANTS.N_DEPOTS}\n" +
    $"number of customers = {CONSTANTS.N_NODES}\n" +
    $"depot creation radius = {CONSTANTS.DEPOT_CREATION_RADIUS}\n" +
    $"number of priority groups = {CONSTANTS.NUMBER_OF_PRIORITY_GROUPS}\n" +
    $"x dimension of the board (used for visual representation) = {CONSTANTS.X_DIM}\n" +
    $"y dimension of the board (used for visual representation) = {CONSTANTS.Y_DIM}\n");
            Console.WriteLine($"RNG seed = {CONSTANTS.RNG_SEED}");
            return new InstanceGeneratorConfig();
        }

        private static void set_rng()
        {
            ++curr_pos;
            try
            {
                if (instanceGeneratorConfig == null)
                {
                    end(1, "Error when setting RNG seed: instance parameters weren't correctly provided, therefore there's no instance to set the seed value.");
                }
                int seed_number = Convert.ToInt32(args[curr_pos]);
                Console.WriteLine(String.Format("Manual seed input detected (-rng). Setting seed to {0}", seed_number));
                instanceGeneratorConfig.RNG = new Random(seed_number);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                end(1);
            }
        }

        private static void set_drawing_options()
        {
            ++curr_pos;
            try
            {
                Directory.CreateDirectory(args[curr_pos]);
                drawing_path = args[curr_pos] + "\\";
                Console.WriteLine($"Setting drawing directory to '{drawing_path}'...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                end(1);
            }
        }

        private static void set_model_runtime()
        {
            ++curr_pos;
            try
            {
                model_runtime = Convert.ToDouble(args[curr_pos]);
                Console.WriteLine($"Setting model time limit to: {model_runtime}s");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                end(1);
            }
        }

        private static void help()
        {
            Console.Write("Example usage: ./PMedians [-f <arg> | -val <...args> | -dflt | -?] [-rng <arg>] [-draw <arg>]\n" +
                "-file (-f)\t Path to the .txt instance file. File must have exactly 10 parameters disposed in individual lines.\n" +
                "-values (-val)\t 10 values corresponding to the instance generation parameters:\n" +
                "\n" +
                "* time periods (int)\n" +
                "* max operating depots per periods (int)\n" +
                "* max customer nodes per depot node (int)\n" +
                "* depot usage cost (double)\n" +
                "* number of depots (int)\n" +
                "* number of customers (int)\n" +
                "* depot creation radius (int)\n" +
                "* number of priority groups, from 0 to 3 (int)\n" +
                "* x dimension of the board (used for visual representation) (int)\n" +
                "* y dimension of the board (used for visual representation) (int)\n" +
                "\n" +
                "-default (-dflt)\t Uses default instance parameter values to create a sample instance\n" +
                "-randomseed (-rng)\t Random Number Generator (RNG) seed (int). Default value = 1000\n" +
                "-draw\t Name of the folder where instance/solution drawings will be put in" +
                "-output (-o)\t Sets a destination file for the output of the model's solution. Prints UB, LB, gap, runtime, number of variables, and number of constraints" +
                "- help (-h) (-?)\t Show program's usage message\n");
            end(0);
        }

        private static void end(int error_code, string message = "\nfinishing...\n")
        {
            Console.WriteLine(String.Format(message));
            Environment.Exit(error_code);
        }
    }

    static class main
    {
        static int Main(string[] args)
        {
            try
            {
                ArgParser.parse_args(args);

                InstanceGeneratorConfig instanceGeneratorConfig = ArgParser.getInstanceGeneratorConfig();
                InstanceGeneratorMain instanceGenerator = new InstanceGenerator.InstanceGeneratorMain(instanceGeneratorConfig);
                instanceGenerator.create_instance();

                PMedian pMedianProblem = new PMedian(instanceGenerator, ArgParser.getDrawingPath() + "PMEDIAN");
                pMedianProblem.setup_problem();
                pMedianProblem.solve_instance(ArgParser.getModelRuntime());
                pMedianProblem.publish_model();

                if (ArgParser.getDrawingPath() != null)
                {
                    InstanceDrawing instanceDrawing = new InstanceDrawing(instanceGenerator, new DrawingSettings());
                    instanceDrawing.draw(ArgParser.getDrawingPath() + "instance");
                    pMedianProblem.draw_solution(instanceDrawing, ArgParser.getDrawingPath() + "solution");
                }

                if (ArgParser.results_filename != null)
                {
                    pMedianProblem.write_results_to_file(ArgParser.results_filename);
                }

                return 0;
                //else
                //{
                //    PMedianController.folder_cleanup(".bmp", ".lp", ".sol");

                //    InstanceGenerator.InstanceGeneratorMain instanceGenerator = new InstanceGenerator.InstanceGeneratorMain(new InstanceGeneratorConfig());
                //    instanceGenerator.create_instance();
                //    InstanceDrawing instanceDrawing = new InstanceDrawing(instanceGenerator, new DrawingSettings());
                //    instanceDrawing.draw("instance");
                //    PMedian pMedianProblem = new PMedian(instanceGenerator, "PMedian");
                //    pMedianProblem.setup_problem();
                //    pMedianProblem.solve_instance(7200);
                //    pMedianProblem.publish_model();
                //    pMedianProblem.draw_solution(instanceDrawing, "solution");

                //    return 0;
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ArgParser.getHelp();
                return 1;
            }
        }
    }
}
