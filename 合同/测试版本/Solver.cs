using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using Google.OrTools.LinearSolver;
using Microsoft.Z3;
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
    public class ORTools_Solver
    {
        public static void Solve()
        {
            // 创建线性规划求解器
            Google.OrTools.LinearSolver.Solver solver = new Google.OrTools.LinearSolver.Solver("LinearProgrammingExample", Google.OrTools.LinearSolver.Solver.OptimizationProblemType.GLOP_LINEAR_PROGRAMMING);

            // 定义变量
            Variable C3 = solver.MakeNumVar(0, double.PositiveInfinity, "C3");
            Variable D3 = solver.MakeNumVar(0, double.PositiveInfinity, "D3");
            Variable E3 = solver.MakeNumVar(0, double.PositiveInfinity, "E3");
            Variable C4 = solver.MakeNumVar(0, double.PositiveInfinity, "C4");
            Variable D4 = solver.MakeNumVar(0, double.PositiveInfinity, "D4");
            Variable E4 = solver.MakeNumVar(0, double.PositiveInfinity, "E4");
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

            Constraint constraint4 = solver.MakeConstraint(8800, 8800);
            constraint4.SetCoefficient(C3, 1);
            constraint4.SetCoefficient(C4, 1);
            constraint4.SetCoefficient(C5, 1);

            Constraint constraint5 = solver.MakeConstraint(8800, 8800);
            constraint5.SetCoefficient(D3, 1);
            constraint5.SetCoefficient(D4, 1);
            constraint5.SetCoefficient(D5, 1);

            Constraint constraint6 = solver.MakeConstraint(8800, 8800);
            constraint6.SetCoefficient(E3, 1);
            constraint6.SetCoefficient(E4, 1);
            constraint6.SetCoefficient(E5, 1);

            /*
            Objective objective = solver.Objective();
            objective.SetCoefficient(C3, 0);
            objective.SetMaximization();  
            */

            // 求解问题
            Google.OrTools.LinearSolver.Solver.ResultStatus resultStatus = solver.Solve();

            switch (resultStatus)
            {
                case Google.OrTools.LinearSolver.Solver.ResultStatus.OPTIMAL:
                    MessageBox.Show("找到最优解");
                    break;
                case Google.OrTools.LinearSolver.Solver.ResultStatus.FEASIBLE:
                    MessageBox.Show("找到可行解");
                    break;
                case Google.OrTools.LinearSolver.Solver.ResultStatus.INFEASIBLE:
                    MessageBox.Show("无解");
                    break;
                case Google.OrTools.LinearSolver.Solver.ResultStatus.UNBOUNDED:
                    MessageBox.Show("无界");
                    break;
                case Google.OrTools.LinearSolver.Solver.ResultStatus.ABNORMAL:
                    MessageBox.Show("错误");
                    break;
                case Google.OrTools.LinearSolver.Solver.ResultStatus.NOT_SOLVED:
                    MessageBox.Show("未求解");
                    break;
            }

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

    public class Z3_Solver
    {
        public static void Solve()
        {
            Context ctx = new Context();
            RealExpr C3 = ctx.MkRealConst("C3");
            RealExpr C4 = ctx.MkRealConst("C4");
            RealExpr C5 = ctx.MkRealConst("C5");
            RealExpr D3 = ctx.MkRealConst("D3");
            RealExpr D4 = ctx.MkRealConst("D4");
            RealExpr D5 = ctx.MkRealConst("D5");
            RealExpr E3 = ctx.MkRealConst("E3");
            RealExpr E4 = ctx.MkRealConst("E4");
            RealExpr E5 = ctx.MkRealConst("E5");

            Microsoft.Z3.Solver s = ctx.MkSolver();
            s.Add(ctx.MkGe(C3, ctx.MkReal(0)));
            s.Add(ctx.MkGe(D3, ctx.MkReal(0)));
            s.Add(ctx.MkGe(E3, ctx.MkReal(0)));
            s.Add(ctx.MkGe(C4, ctx.MkReal(0)));
            s.Add(ctx.MkGe(D4, ctx.MkReal(0)));
            s.Add(ctx.MkGe(E4, ctx.MkReal(0)));
            s.Add(ctx.MkGe(C5, ctx.MkReal(0)));
            s.Add(ctx.MkGe(D5, ctx.MkReal(0)));
            s.Add(ctx.MkGe(E5, ctx.MkReal(0)));

            s.Add(ctx.MkEq(ctx.MkAdd(C3, D3, E3), ctx.MkReal("10000.0")));
            s.Add(ctx.MkEq(ctx.MkAdd(C4, D4, E4), ctx.MkReal("5000.0")));
            s.Add(ctx.MkEq(ctx.MkAdd(C5, D5, E5), ctx.MkReal("10000.0")));
            s.Add(ctx.MkEq(ctx.MkAdd(C3, C4, C5), ctx.MkReal("8800.0")));
            s.Add(ctx.MkEq(ctx.MkAdd(D3, D4, D5), ctx.MkReal("8800.0")));
            s.Add(ctx.MkEq(ctx.MkAdd(E3, E4, E5), ctx.MkReal("8800.0")));

            if (s.Check() == Status.SATISFIABLE)
            {
                Model m = s.Model;
                MessageBox.Show("C3 = " + m.Evaluate(C3));
                MessageBox.Show("C4 = " + m.Evaluate(C4));
                MessageBox.Show("C5 = " + m.Evaluate(C5));
                MessageBox.Show("D3 = " + m.Evaluate(D3));
                MessageBox.Show("D4 = " + m.Evaluate(D4));
                MessageBox.Show("D5 = " + m.Evaluate(D5));
                MessageBox.Show("E3 = " + m.Evaluate(E3));
                MessageBox.Show("E4 = " + m.Evaluate(E4));
                MessageBox.Show("E5 = " + m.Evaluate(E5));
            }
            else
            {
                MessageBox.Show("No solution found.");
            }
        }
    }



}
