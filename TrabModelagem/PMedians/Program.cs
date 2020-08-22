using System;
using System.CodeDom.Compiler;
using System.IO.Enumeration;
using Gurobi;
using InstanceGenerator;

namespace PMedians
{
    class PMedianConfig
    {
        // to do
    }

    class PMedian
    {
        private readonly InstanceGenerator.InstanceGenerator Instance;
        private Gurobi.GRBEnv env;
        private Gurobi.GRBModel Model;
        private string filename = "PMEDIAn";

        public PMedian(InstanceGenerator.InstanceGenerator pInstance, string pfilename = "")
        {
            Instance = pInstance;
            filename = pfilename;
        }

        private void setup_env(string filename)
        {
            env = new GRBEnv(filename);
        }
        private void setup_model()
        {
            Model = new GRBModel(env);
        }
        private void setup_problem()
        {

        }

        public void draw_instance()
        {
            // to do
        }
        public void solve_instance()
        {

        }
        public void draw_solution()
        {

        }
    }


    class main
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
