using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Gurobi;
using InstanceGenerator;
using System.Drawing;
using System.Diagnostics;
using System.IO;

namespace PMedians
{
    public class VariableGenerator
    {
        private readonly InstanceGenerator.InstanceGenerator InstData;
        private Gurobi.GRBModel Model;
        public GRBVar[,] depot_usage;
        public GRBVar[,,] customer_depot_designation;

        public VariableGenerator(InstanceGenerator.InstanceGenerator pInstData, Gurobi.GRBModel pModel)
        {
            InstData = pInstData;
            Model = pModel;
            depot_usage = new GRBVar[InstData.getInstanceConfig().n_depots, InstData.getInstanceConfig().time_periods];
            customer_depot_designation = new GRBVar[InstData.getInstanceConfig().n_nodes, InstData.getInstanceConfig().n_depots, InstData.getInstanceConfig().time_periods];
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
        public void make_all_vars()
        {
            this.create_depot_usage_vars();
            this.create_depot_customer_designation_vars();
        }
    }

    public class ConstraintGenerator
    {
        private GRBModel Model;
        private readonly VariableGenerator variableGenerator;
        private readonly InstanceGenerator.InstanceGenerator instanceGenerator;

        public ConstraintGenerator(GRBModel pModel, VariableGenerator pvariableGenerator, InstanceGenerator.InstanceGenerator pinstanceGenerator)
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

        public void make_all_constraints()
        {
            this.max_depot_nodes_per_period(instanceGenerator.getInstanceConfig().max_depot_nodes_per_period);
            //this.max_nodes_per_depot(instanceGenerator.getInstanceConfig().max_nodes_per_depot);
            this.one_visit_per_node();
            this.service_only_by_active_depot();

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
        private readonly InstanceGenerator.InstanceGenerator Instance;
        private VariableGenerator variableGenerator;
        private ConstraintGenerator constraintGenerator;
        private string filename;

        public PMedian(InstanceGenerator.InstanceGenerator pInstance, string pfilename = "PMEDIAN.log")
            : base(new GRBEnv(pfilename))
        {
            Instance = pInstance;
            filename = pfilename;
        }

        public InstanceGenerator.InstanceGenerator getInstanceGenerator()
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

        public int solve_instance()
        {
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
            this.Write("Infeasible.ilp");
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

    static class main
    {
        static void Main(string[] args)
        {
            PMedianController.folder_cleanup(".bmp", ".lp", ".sol");

            InstanceGenerator.InstanceGenerator instanceGenerator = new InstanceGenerator.InstanceGenerator(new InstanceGeneratorConfig());
            instanceGenerator.create_instance();
            InstanceDrawing instanceDrawing = new InstanceDrawing(instanceGenerator, new DrawingSettings());
            instanceDrawing.draw("instance.bmp");
            PMedian pMedianProblem = new PMedian(instanceGenerator, "PMedian.log");
            pMedianProblem.setup_problem();
            pMedianProblem.solve_instance();
            pMedianProblem.publish_model();
            pMedianProblem.draw_solution(instanceDrawing, "solution");

        }
    }
}
