using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Neher
{
    internal class NeherCalc
    {
        public (double, double, double) ysCalc(Cable cabo)
        {
            double Rdc_Tc = 0;
            if (cabo.type == "Low Voltage" || cabo.type == "VFD")
            {
                Rdc_Tc = (cabo.Rdc_25C * 1.25545) / 304.8;
            }
            else
            {
                Rdc_Tc = (cabo.Rdc_25C * 1.3144) / 304.8;
            }

            double Xs = 2 * Math.PI * 60 * Math.Pow(10, -7) / Rdc_Tc;

            double Ys = Math.Pow(Xs, 2) / (100 + (0.8 * Math.Pow(Xs, 2)));

            return (Xs, Ys, Rdc_Tc);

        }

        public (double Kp, double Yp) ypCalc(Cable cabo, NeherElement elementoNeher)
        {
            double dcSobreS = cabo.diamCond / cabo.OD;
            double dcSobreS_2 = dcSobreS * dcSobreS;
            double dcSobreS_4 = dcSobreS_2 * dcSobreS_2;

            double Kp = dcSobreS_2 * (0.312 * dcSobreS_2 + (0.27 / dcSobreS_4) + 0.31);

            double Xp = 2.8;
            double Yp = (Xp * Xp) / (100 + 0.8 * Xp * Xp) * Kp;

            return (Kp, Yp);
        }

        public double riCalc(Cable cabo)
        {
            double ri = 0.00796 * Math.Log( (cabo.diamCond + 2*(cabo.sizeInsul/1000)) / cabo.diamCond);

            return ri;
        }
        public double CalcularReMutuo(double ps, double h, double Dod_polegadas, double Sij, int qt, int pos)
        {
            // Converter polegadas para metros
            double Dod_metros = Dod_polegadas * 0.0254;
            double raio_m = Dod_metros / 2.0;

            // Condutividade térmica do solo (λ)
            double lambda = 1.0 / ps;

            double Re_m = 0;

            // Índice do eletroduto de interesse: pos (1-based)
            int i = pos - 1;

            for (int j = 0; j < qt; j++)
            {
                if (j == i)
                    continue;

                double distancia = Math.Abs(j - i) * Sij;

                Re_m += (1 / (2 * Math.PI * lambda)) * Math.Log(distancia / raio_m);
            }

            return Re_m; // em °C·m/W
        }

        public double CalcularRe(double ps, double h, double Dod_polegadas)
        {
            double Dod_m = Dod_polegadas * 0.0254; // Converter polegadas para metros
            double Re = (ps / (2 * Math.PI)) * Math.Log((4 * h) / Dod_m);
            return Re; // Resultado em °C·m/W
        }
        public double CalcularCorrenteAdmissivel(double Tc, double Ta, double deltaTd, int n, double Rac, double Rca)
        {
            // Fórmula de Neher-McGrath para corrente admissível
            double numerador = Tc - Ta - deltaTd;
            double denominador = n * Rac * Rca;

            if (denominador <= 0 || numerador <= 0)
                return 0; // Evitar raiz de negativo

            double I = 1000 * Math.Sqrt(numerador / denominador);
            return I;
        }



    }

}

