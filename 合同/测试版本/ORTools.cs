using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Constraint = Google.OrTools.LinearSolver.Constraint;

namespace SMGI_Common
{
    public class ORTools
    {

        public static void Solve()
        {
            // 创建线性规划求解器
            Solver solver = new Solver("LinearProgrammingExample", Solver.OptimizationProblemType.GLOP_LINEAR_PROGRAMMING);

            // 定义变量
            Variable C3 = solver.MakeNumVar(0, double.PositiveInfinity, "C3");
            Variable D3 = solver.MakeNumVar(0, double.PositiveInfinity, "D3");
            Variable E3= solver.MakeNumVar(0, double.PositiveInfinity, "E3");
            Variable C4 = solver.MakeNumVar(0, double.PositiveInfinity, "C4");
            Variable D4 = solver.MakeNumVar(0, double.PositiveInfinity, "D4");
            Variable E4= solver.MakeNumVar(0, double.PositiveInfinity, "E4");
            Variable C5 = solver.MakeNumVar(0, double.PositiveInfinity, "C5");
            Variable D5 = solver.MakeNumVar(0, double.PositiveInfinity, "D5");
            Variable E5 = solver.MakeNumVar(0, double.PositiveInfinity, "E5");

            // 添加约束条件
            Constraint constraint1 = solver.MakeConstraint(10000, 10000);
            constraint1.SetCoefficient(C3, 1);
            constraint1.SetCoefficient(D3, 1);
            constraint1.SetCoefficient(E3, 1);

            Constraint constraint2 = solver.MakeConstraint(5000, 5000);
            constraint2.SetCoefficient(C4, 1);
            constraint2.SetCoefficient(D4, 1);
            constraint2.SetCoefficient(E4, 1);

            Constraint constraint3 = solver.MakeConstraint(10000, 10000);
            constraint3.SetCoefficient(C5, 1);
            constraint3.SetCoefficient(D5, 1);
            constraint3.SetCoefficient(E5, 1);

            Constraint constraint4 = solver.MakeConstraint(0, 8800);
            constraint4.SetCoefficient(C3, 1);
            constraint4.SetCoefficient(C4, 1);
            constraint4.SetCoefficient(C5, 1);

            Constraint constraint5 = solver.MakeConstraint(0, 8800);
            constraint5.SetCoefficient(D3, 1);
            constraint5.SetCoefficient(D4, 1);
            constraint5.SetCoefficient(D5, 1);

            Constraint constraint6 = solver.MakeConstraint(0, 8800);
            constraint6.SetCoefficient(E3, 1);
            constraint6.SetCoefficient(E4, 1);
            constraint6.SetCoefficient(E5, 1);

            /*
            Objective objective = solver.Objective();
            objective.SetCoefficient(C3, 0);
            objective.SetMaximization();  
            */

            // 求解问题
            solver.Solve();

            // 输出结果
            MessageBox.Show("C3 = " + C3.SolutionValue());
            MessageBox.Show("C4 = " + C4.SolutionValue());
            MessageBox.Show("C5 = " + C5.SolutionValue());
            MessageBox.Show("D3 = " + D3.SolutionValue());
            MessageBox.Show("D4 = " + D4.SolutionValue());
            MessageBox.Show("D5 = " + D5.SolutionValue());
            MessageBox.Show("E3 = " + E3.SolutionValue());
            MessageBox.Show("E4 = " + E4.SolutionValue());
            MessageBox.Show("E5 = " + E5.SolutionValue());


        }
    }

    

}
