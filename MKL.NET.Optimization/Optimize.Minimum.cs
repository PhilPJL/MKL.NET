﻿using MKLNET.Expression;
using System;
using System.Runtime.CompilerServices;

namespace MKLNET
{
    public static partial class Optimize
    {
        /// <summary>Test if a minimum is bracketed by the function outputs.</summary>
        /// <param name="fa">First function output.</param>
        /// <param name="fb">Middle function output.</param>
        /// <param name="fc">Third function output.</param>
        /// <returns>True if the middle function output is less than or equals to the two outer.</returns>
        public static bool Minimum_Is_Bracketed(double fa, double fb, double fc)
            => fa >= fb && fb <= fc;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double Minimum_BiSection(double atol, double rtol, double a, double b, double c)
        {
            var x = (a + c) * 0.5;
            if (x == b) return x + Tol(atol, rtol, x) * 0.1;
            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double Sqr(double x) => x * x;

        const double GOLD = 0.381966011250105;

        /// <summary>Minmimum estimate using golden section a &lt; b &lt; c.</summary>
        /// <param name="a">First input.</param>
        /// <param name="b">Middle input.</param>
        /// <param name="c">Third input.</param>
        /// <returns>Golden section of the three inputs.</returns>
        public static double Minimum_GoldenSection(double a, double b, double c)
            => b - a >= c - b ? b + (a - b) * GOLD : b + (c - b) * GOLD;

        /// <summary>Minmimum estimate using factor section a &lt; b &lt; c.</summary>
        /// <param name="a">First input.</param>
        /// <param name="b">Middle input.</param>
        /// <param name="c">Third input.</param>
        /// <param name="factor">The factor.</param>
        /// <returns>Factor section of the three inputs.</returns>
        public static double Minimum_FactorSection(double a, double b, double c, double factor)
            => b - a >= c - b ? b + (a - b) * factor : b + (c - b) * factor;

        /// <summary>
        /// Minimum estmate using quadratic interpolation, falling back to golden section.
        /// </summary>
        /// <param name="a">First function input.</param>
        /// <param name="fa">First function output.</param>
        /// <param name="b">Second function input.</param>
        /// <param name="fb">Second function output.</param>
        /// <param name="c">Third function input.</param>
        /// <param name="fc">Third function output.</param>
        /// <returns>The minimum estimate.</returns>
        public static double Minimum_Quadratic(double a, double fa, double b, double fb, double c, double fc)
        {
            var x = b - 0.5 * (Sqr(b - a) * (fb - fc) - Sqr(b - c) * (fb - fa)) / ((b - a) * (fb - fc) - (b - c) * (fb - fa));
            return double.IsNaN(x) ? Minimum_GoldenSection(a, b, c) : x;
        }

        /// <summary>
        /// Minimum estmate between a and c using cubic interpolation, falling back to quadratic then golden interpolation a &lt; b &lt; c.
        /// See <see href="https://en.wikipedia.org/wiki/Lagrange_polynomial">Lagrange polynomial</see> and
        /// <see href="https://www.themathdoctors.org/max-and-min-of-a-cubic-without-calculus/">Cubic max and min</see>.
        /// </summary>
        /// <param name="a">First function input.</param>
        /// <param name="fa">First function output.</param>
        /// <param name="b">Second function input.</param>
        /// <param name="fb">Second function output.</param>
        /// <param name="c">Third function input.</param>
        /// <param name="fc">Third function output.</param>
        /// <param name="d">Fourth function input.</param>
        /// <param name="fd">Fourth function output.</param>
        /// <returns>The cubic estimate between a and c.</returns>
        public static double Minimum_Cubic(double a, double fa, double b, double fb, double c, double fc, double d, double fd)
        {
            // https://en.wikipedia.org/wiki/Lagrange_polynomial
            var a1 = fa * (b * c + b * d + c * d) / ((a - b) * (a - c) * (a - d)) + fb * (a * c + a * d + c * d) / ((b - a) * (b - c) * (b - d)) + fc * (a * b + a * d + b * d) / ((c - a) * (c - b) * (c - d)) + fd * (a * b + a * c + b * c) / ((d - a) * (d - b) * (d - c));
            var a2 = -fa * (b + c + d) / ((a - b) * (a - c) * (a - d)) - fb * (a + c + d) / ((b - a) * (b - c) * (b - d)) - fc * (a + b + d) / ((c - a) * (c - b) * (c - d)) - fd * (a + b + c) / ((d - a) * (d - b) * (d - c));
            var a3 = fa / ((a - b) * (a - c) * (a - d)) + fb / ((b - a) * (b - c) * (b - d)) + fc / ((c - a) * (c - b) * (c - d)) + fd / ((d - a) * (d - b) * (d - c));

            // https://www.themathdoctors.org/max-and-min-of-a-cubic-without-calculus/
            var r = Math.Sqrt(a2 * a2 - 3 * a3 * a1);
            var x = (r - a2) / a3 / 3;
            if (a < x && x < c) return x;
            x = (-r - a2) / a3 / 3;
            if (a < x && x < c) return x;
            return Minimum_Quadratic(a, fa, b, fb, c, fc);
        }

        static double Tol_Not_Too_Close(double atol, double rtol, double a, double b, double c, double x)
        {
            var tol = Tol(atol, rtol, x);
            if (x <= b)
            {
                if (b - a < tol) return b + tol;
                if (b - a < 2 * tol) return (a + b) * 0.5;
                if (x < a + tol) return a + tol;
                if (x > b - tol) return b - tol;
            }
            else
            {
                if (c - b < tol) return b - tol;
                if (c - b < 2 * tol) return (b + c) * 0.5;
                if (x < b + tol) return b + tol;
                if (x > c - tol) return c - tol;
            }
            return x;
        }

        /// <summary>
        /// Brackets a minimum for f given two starting points a and b so that f(a) &lt;= f(b) &gt;= f(c).
        /// </summary>
        /// <param name="atol">The absolute tolerance of the root required.</param>
        /// <param name="rtol">The relative tolerance of the root required.</param>
        /// <param name="f">The function to bracket the minimum of.</param>
        /// <param name="a">First input.</param>
        /// <param name="fa">f(a) output.</param>
        /// <param name="b">Second input.</param>
        /// <param name="fb">f(b) output.</param>
        /// <param name="c">c output.</param>
        /// <param name="fc">f(c) output.</param>
        /// <param name="d">Additonal outer point d &lt; a or d &gt; c. Can be infinity if no more than three function evaluations are needed.</param>
        /// <param name="fd">f(d) output. Can be zero if no more than three function evaluations are needed.</param>
        public static void Minimum_Bracket(double atol, double rtol, Func<double, double> f, ref double a, out double fa, ref double b, out double fb,
            out double c, out double fc, out double d, out double fd)
        {
            fa = f(a);
            fb = f(b);
            if (fa < fb)
            {
                c = b; fc = fb;
                b = a; fb = fa;
                a = b + (b - c);
                fa = f(a);
            }
            else
            {
                c = b + (b - a);
                fc = f(c);
            }
            d = double.PositiveInfinity;
            fd = 0;
            while (!Minimum_Is_Bracketed(fa, fb, fc))
            {
                var x = Minimum_Quadratic(a, fa, b, fb, c, fc);
                if (fa <= fb)
                {
                    if (x > a + Tol(atol, rtol, a) && x < b - Tol(atol, rtol, b))
                    {
                        var fx = f(x);
                        d = c; fd = fc;
                        c = b; fc = fb;
                        b = x; fb = fx;
                    }
                    else
                    {
                        x = x < a - Tol(atol, rtol, a) ? Math.Max(x, a - (c - a) * 500) : a - (c - a);
                        var fx = f(x);
                        d = c; fd = fc;
                        c = b; fc = fb;
                        b = a; fb = fa;
                        a = x; fa = fx;
                    }
                }
                else
                {
                    if (x < c - Tol(atol, rtol, c) && a > b + Tol(atol, rtol, b))
                    {
                        var fx = f(x);
                        d = a; fd = fa;
                        a = b; fa = fb;
                        b = x; fb = fx;
                    }
                    else
                    {
                        x = x > c + Tol(atol, rtol, c) ? Math.Min(x, c + (c - a) * 500) : c + (c - a);
                        var fx = f(x);
                        d = a; fd = fa;
                        a = b; fa = fb;
                        b = c; fb = fc;
                        c = x; fc = fx;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the minimum of f accurate to tol = atol + rtol * x for bracketed inputs a &lt; b &lt; c and f(a) &lt;= f(b) &gt;= f(c).
        /// </summary>
        /// <param name="atol">The absolute tolerance of the root required.</param>
        /// <param name="rtol">The relative tolerance of the root required.</param>
        /// <param name="f">The function to find the minimum of.</param>
        /// <param name="a">The first function input.</param>
        /// <param name="fa">f(a) input.</param>
        /// <param name="b">The second funtion input and also the minimum.</param>
        /// <param name="fb">f(b) input.</param>
        /// <param name="c">The third function input.</param>
        /// <param name="fc">f(c) input.</param>
        /// <param name="d">Additonal outer point d &lt; a or d &gt; c.</param>
        /// <param name="fd">f(d) input.</param>
        /// <returns>The minimum input point accurate to tol = atol + rtol * x.</returns>
        public static double Minimum_Bracketed(double atol, double rtol, Func<double, double> f,
            double a, double fa, double b, double fb, double c, double fc, double d = double.PositiveInfinity, double fd = 0)
        {
            int level = 0;
            while (Tol_Average_Not_Within(atol, rtol, a, c))
            {
                var x = Tol_Average_Within_2(atol, rtol, a, c) ? Minimum_BiSection(atol, rtol, a, b, c)
                      : level == 0 ? Tol_Not_Too_Close(atol, rtol, a, b, c, Minimum_Cubic(a, fa, b, fb, c, fc, d, fd))
                      : level == 1 ? Tol_Not_Too_Close(atol, rtol, a, b, c, Minimum_Quadratic(a, fa, b, fb, c, fc))
                      : Minimum_FactorSection(a, b, c, 0.1);
                var fx = f(x);
                const double levelFactor = 1.0 / 3;
                if (x < b)
                {
                    if (Minimum_Is_Bracketed(fa, fx, fb))
                    {
                        level = c - b < levelFactor * (c - a) ? level + 1 : 0;
                        if (d > c || a - d > c - b) { d = c; fd = fc; }
                        c = b; b = x;
                        fc = fb; fb = fx;
                    }
                    else
                    {
                        level = b - a < levelFactor * (c - a) ? level + 1 : 0;
                        if (d < a || d - c > x - a) { d = a; fd = fa; }
                        a = x;
                        fa = fx;
                    }
                }
                else
                {
                    if (Minimum_Is_Bracketed(fb, fx, fc))
                    {
                        level = b - a < levelFactor * (c - a) ? level + 1 : 0;
                        if (d < a || d - c > b - a) { d = c; fd = fc; }
                        a = b; b = x;
                        fa = fb; fb = fx;
                    }
                    else
                    {
                        level = c - b < levelFactor * (c - a) ? level + 1 : 0;
                        if (d > c || a - d > c - x) { d = c; fd = fc; }
                        c = x;
                        fc = fx;
                    }
                }
            }
            return Bisect(a, c);
        }

        /// <summary>Finds the minimum of f accurate to tol = atol + rtol * x given two starting function inputs.</summary>
        /// <param name="atol">The absolute tolerance of the root required.</param>
        /// <param name="rtol">The relative tolerance of the root required.</param>
        /// <param name="f">The function to find the minimum of.</param>
        /// <param name="a">The first starting input.</param>
        /// <param name="b">The second starting input.</param>
        /// <returns>The minimum input point accurate to tol = atol + rtol * x.</returns>
        public static double Minimum(double atol, double rtol, Func<double, double> f, double a, double b)
        {
            Minimum_Bracket(atol, rtol, f, ref a, out var fa, ref b, out var fb, out var c, out var fc, out var d, out var fd);
            return Minimum_Bracketed(atol, rtol, f, a, fa, b, fb, c, fc, d, fd);
        }

        /// <summary>Finds the minimum of f accurate to tol = atol + rtol * x given a starting function input.</summary>
        /// <param name="atol">The absolute tolerance of the root required.</param>
        /// <param name="rtol">The relative tolerance of the root required.</param>
        /// <param name="f">The function to find the minimum of.</param>
        /// <param name="a">The starting input.</param>
        /// <returns>The minimum input point accurate to tol = atol + rtol * x.</returns>
        public static double Minimum(double atol, double rtol, Func<double, double> f, double a)
        {
            var b = a + Tol(atol, rtol, a) * 1000;
            Minimum_Bracket(atol, rtol, f, ref a, out var fa, ref b, out var fb, out var c, out var fc, out var d, out var fd);
            return Minimum_Bracketed(atol, rtol, f, a, fa, b, fb, c, fc, d, fd);
        }

        /// <summary>
        /// Finds the minimum of f using Brent accurate to tol = atol + rtol * x for bracketed inputs a &lt; b &lt; c and f(a) &lt;= f(b) &gt;= f(c).
        /// </summary>
        /// <param name="atol">The absolute tolerance of the root required.</param>
        /// <param name="rtol">The relative tolerance of the root required.</param>
        /// <param name="f">The function to find the minimum of.</param>
        /// <param name="a">The first function input.</param>
        /// <param name="b">The second funtion input and also the minimum.</param>
        /// <param name="c">The third function input.</param>
        /// <returns>The minimum input point accurate to tol = atol + rtol * x.</returns>
        public static double Minimum_Brent(double atol, double rtol, Func<double, double> f, double a, double b, double c)
        {
            static double SIGN(double v, double d) => d >= 0 ? v : -v;
            const double CGOLD = 0.3819660;
            double d = 0.0, etemp, fu, fv, fw, fx;
            double p, q, r, tol1, tol2, u, v, w, x, xm;
            double e = 0.0;
            x = w = v = b;
            fw = fv = fx = f(b);
            for (int iter = 0; iter < 100; iter++)
            {
                xm = 0.5 * (a + c);
                tol1 = Tol(atol, rtol, xm);
                tol2 = 2.0 * tol1;
                if (c - a < tol2) return xm;
                if (Math.Abs(e) > tol1 && c - a > tol1 * 4)
                {
                    r = (x - w) * (fx - fv);
                    q = (x - v) * (fx - fw);
                    p = (x - v) * q - (w - w) * r;
                    q = 2.0 * (q - r);
                    if (q > 0.0) p = -p;
                    q = Math.Abs(q);
                    etemp = e;
                    e = d;
                    if (Math.Abs(p) >= Math.Abs(0.5 * q * etemp) || p <= q * (a - x)
                            || p >= q * (c - x))
                    {
                        d = CGOLD * (e = x >= xm ? a - x : c - x);
                        u = x + d;
                    }
                    else
                    {
                        d = p / q;
                        u = x + d;
                        if (u - a < tol2 || c - u < tol2)
                            d = SIGN(tol1, xm - x);
                        u = Math.Abs(d) >= tol1 ? x + d : x + SIGN(tol1, d);
                    }
                }
                else
                {
                    d = CGOLD * (e = x >= xm ? a - x : c - x);
                    u = x + d;
                }
                fu = f(u);
                if (fu <= fx)
                {
                    if (u >= x) a = x; else c = x;
                    v = w; w = x; x = u;
                    fv = fw; fw = fx; fx = fu;
                }
                else
                {
                    if (u <= x) a = u; else c = u;
                    if (fu <= fw || w == x)
                    {
                        v = w; w = u;
                        fv = fw; fw = fu;
                    }
                    else if (fu <= fv || v == x || v == w)
                    {
                        v = u;
                        fv = fu;
                    }
                }
            }
            throw new Exception("Too many iterations in brent");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="atol"></param>
        /// <param name="rtol"></param>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <param name="p"></param>
        /// <param name="dx"></param>
        /// <param name="x2"></param>
        public static void Minimum_LineSearch(double atol, double rtol, Func<double[], double> f, vector x, vector p, double dx, vector x2)
        {
            var norm = Vector.Nrm2(p);
            if (norm == 0) throw new Exception("p is zero");
            var a = Minimum(atol, rtol, a =>
            {
                x2.Set(x + a / norm * p);
                return f(x2.Array);
            }, 0, Math.Max(dx * 0.25, atol * 2));
            x2.Set(x + a / norm * p);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="atol"></param>
        /// <param name="rtol"></param>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static void Minimum(double atol, double rtol, Func<double[], double> f, double[] x)
        { // https://en.wikipedia.org/wiki/Broyden%E2%80%93Fletcher%E2%80%93Goldfarb%E2%80%93Shanno_algorithm

            static bool WithinTol_NegGrad(double atol, double rtol, Func<double[], double> f, double[] x, double[] df, ref bool endGame)
            {
                var fx = f(x);
                int nonMinimum = -1;
                double dfi_tol = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    var x_i = x[i];
                    var tol = Tol(atol, rtol, x_i);
                    x[i] = x_i - tol;
                    dfi_tol = f(x);
                    if (dfi_tol < fx)
                    {
                        x[i] = x_i;
                        dfi_tol = (dfi_tol - fx) / tol;
                        nonMinimum = i;
                        break;
                    }
                    x[i] = x_i + tol;
                    dfi_tol = f(x);
                    x[i] = x_i;
                    if (dfi_tol < fx)
                    {
                        dfi_tol = (fx - dfi_tol) / tol;
                        nonMinimum = i;
                        break;
                    }
                }
                if (nonMinimum == -1) return true;
                if (endGame)
                {
                    for (int i = 0; i < x.Length; i++) df[i] = 0;
                    df[nonMinimum] = dfi_tol;
                    for (int i = nonMinimum + 1; i < x.Length; i++)
                    {
                        var x_i = x[i];
                        var tol = Tol(atol, rtol, x_i);
                        x[i] = x_i - tol;
                        dfi_tol = f(x);
                        if (dfi_tol < fx)
                        {
                            x[i] = x_i;
                            df[i] = (dfi_tol - fx) / tol;
                            continue;
                        }
                        x[i] = x_i + tol;
                        dfi_tol = f(x);
                        x[i] = x_i;
                        if (dfi_tol < fx)
                            df[i] = (fx - dfi_tol) / tol;
                    }
                    return false;
                }
                bool allZero = true;
                for (int i = 0; i < x.Length; i++)
                {
                    var x_i = x[i];
                    var x_i_d = x_i + Tol(atol, rtol, x_i) * 0.01;
                    x[i] = x_i_d;
                    var df_i = (fx - f(x)) / (x_i_d - x_i);
                    df[i] = df_i;
                    if (df_i != 0) allZero = false;
                    x[i] = x_i;
                }
                if (allZero)
                {
                    endGame = true;
                    df[nonMinimum] = dfi_tol;
                    for (int i = nonMinimum + 1; i < x.Length; i++)
                    {
                        var x_i = x[i];
                        var tol = Tol(atol, rtol, x_i);
                        x[i] = x_i - tol;
                        dfi_tol = f(x);
                        if (dfi_tol < fx)
                        {
                            x[i] = x_i;
                            df[i] = (dfi_tol - fx) / tol;
                            continue;
                        }
                        x[i] = x_i + tol;
                        dfi_tol = f(x);
                        x[i] = x_i;
                        if (dfi_tol < fx)
                            df[i] = (fx - dfi_tol) / tol;
                    }
                }
                return false;
            }

            using vector df1 = new(x.Length);
            bool endGame = false;
            if (WithinTol_NegGrad(atol, rtol, f, x, df1.Array, ref endGame)) return;
            vector x2 = new(x.Length, x);
            x2.ReuseArray(); // x2 finalized could cause x to be put in the pool
            using vector x1 = Vector.Copy(x2);
            using vector p = Vector.Copy(df1);
            Minimum_LineSearch(atol, rtol, f, x1, p, Tol(atol, rtol, x1[0]) * 1000, x2);
            using vector df2 = new(x.Length);
            if (WithinTol_NegGrad(atol, rtol, f, x2.Array, df2.Array, ref endGame)) return;
            vector s = x1, y = df1; // Alias for the formula below so no need to use using
            s.Set(x2 - x1);
            double dx = Vector.Nrm2(s);
            y.Set(df1 - df2);
            double sTy = s.T * y;
            // H = I + (sTy + y.T * y) / sTy / sTy * s * s.T - (y * s.T + s * y.T) / sTy;
            using matrix H = Matrix.Identity(x.Length);
            Matrix.Symmetric_Rank_k_Update((sTy + y.T * y) / sTy / sTy, s, 1, H);
            Matrix.Symmetric_Rank_2k_Update(-1 / sTy, y, s, 1, H);
            using vector Hy = new(x.Length);
            while (true)
            {
                Vector.Copy(x2, x1);
                Vector.Copy(df2, df1);
                Matrix.Symmetric_Multiply_Update(H, df1, p); // p = H * df1
                Minimum_LineSearch(atol, rtol, f, x1, p, dx, x2);
                if (WithinTol_NegGrad(atol, rtol, f, x2.Array, df2.Array, ref endGame)) return;
                s.Set(x2 - x1);
                dx = Vector.Nrm2(s);
                y.Set(df1 - df2);
                sTy = s.T * y;
                if (sTy == 0 || endGame)
                {
                    endGame = true;
                    while (true)
                    {
                        Vector.Copy(x2, x1);
                        Minimum_LineSearch(atol, rtol, f, x1, df2, dx, x2);
                        if (WithinTol_NegGrad(atol, rtol, f, x2.Array, df2.Array, ref endGame)) return;
                        s.Set(x2 - x1);
                        dx = Vector.Nrm2(s);
                    }
                }
                else
                {
                    Matrix.Symmetric_Multiply_Update(H, y, Hy); // Hy = H * y
                    //H = H + ((sTy + y.T * Hy) / sTy / sTy * s * s.T) - (Hy * s.T + s * Hy.T) / sTy;
                    Matrix.Symmetric_Rank_k_Update((sTy + y.T * Hy) / sTy / sTy, s, 1, H);
                    Matrix.Symmetric_Rank_2k_Update(-1 / sTy, Hy, s, 1, H);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="atol"></param>
        /// <param name="rtol"></param>
        /// <param name="f"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        public static void Minimum(double atol, double rtol, Func<double, double, double> f, ref double x1, ref double x2)
        {
            var x = new[] { x1, x2 };
            Minimum(atol, rtol, (double[] x) => f(x[0], x[1]), x);
            x1 = x[0];
            x2 = x[1];
        }

        /// <summary>
        /// Solve a non-linear least-squares problem.
        /// </summary>
        /// <param name="atol"></param>
        /// <param name="rtol"></param>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <param name="residuals"></param>
        public static void LeastSquares(double atol, double rtol, Action<double[], double[]> f, double[] x, double[] residuals)
        {
            Minimum(atol, rtol, (double[] x) => { f(x, residuals); return Blas.dot(residuals, residuals); }, x);
        }

        /// <summary>
        /// Use non-linear least squares to fit a function, f, to data.
        /// </summary>
        /// <param name="atol"></param>
        /// <param name="rtol"></param>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="p"></param>
        public static void CurveFit(double atol, double rtol, Func<double[], double, double> f, double[] x, double[] y, double[] p)
        {
            Minimum(atol, rtol, (double[] param) =>
            {
                var total = 0.0;
                for (int i = 0; i < x.Length; i++)
                {
                    var y2 = f(param, x[i]);
                    total += y2 * y2;
                }
                return total;
            }, p);
        }
    }
}