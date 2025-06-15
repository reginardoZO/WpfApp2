using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.Attributes;
using static WpfApp2.Cables.Cables_Sizing;

namespace WpfApp2.Cables
{
    class calcCables
    {
        CablesQuery.CablesQueryEngine queryCables = new();

        public double nominalCurrent(inputData dadosEntrada)
        {
            if (dadosEntrada.loadType == "Feeder")
            {
                return dadosEntrada.power;
            }
            else if (dadosEntrada.loadType == "Heater")
            {
                return (dadosEntrada.power * 1000 / (Math.Sqrt(3) * dadosEntrada.voltageLevel));

            }
            else
            {

                if (dadosEntrada.powerUnit == "HP")
                {

                    return ((dadosEntrada.power * 745.6) / (Math.Sqrt(3) * dadosEntrada.voltageLevel * dadosEntrada.powerFactor * dadosEntrada.efficiency / 100));
                }
                else
                {
                    return ((dadosEntrada.power * 1000) / (Math.Sqrt(3) * dadosEntrada.voltageLevel * dadosEntrada.powerFactor * dadosEntrada.efficiency / 100));
                }
            }


        }

        public double temperatureFactor(inputData dadosEntrada)
        {

            double currentFactor = Math.Sqrt((dadosEntrada.cableTempCol - converToCelcius(dadosEntrada.ambientTemperature)) / (dadosEntrada.cableTempCol - 30));

            return currentFactor;

        }

        public double converToCelcius(double tempF)
        {
            return (tempF - 32) * 5 / 9;

        }

        public string findCableByCorrectecCurrent(inputData dadosEntrada, outputData dadosSaida)
        {

            string aux = "";

            if (dadosEntrada.cableTempCol == 75)
                aux = "amp75";
            else
                aux = "amp90";

            if (dadosEntrada.cableType == "VFD")
            {

                int multiplicador = 1;

            volta:

                DataTable retorno = queryCables.ExecuteQuery($"Select size, grndSize from vfd where {aux} >= {dadosSaida.correctedCurrent / multiplicador}");

                if (retorno.Rows.Count == 0)
                {
                    multiplicador++;

                    goto volta;
                }
                else
                {
                    string selectedCable = multiplicador + " x (" + retorno.Rows[0]["size"].ToString() + " + " + retorno.Rows[0]["grndSize"].ToString() + ")";

                    return selectedCable;
                }

            }

            else if (dadosEntrada.cableType == "Multicable")
            {

                int multiplicador = 1;

            volta:

                DataTable retorno = queryCables.ExecuteQuery($"Select size, grndSize from multi where {aux} >= {dadosSaida.correctedCurrent / multiplicador}");

                if (retorno.Rows.Count == 0)
                {
                    multiplicador++;

                    goto volta;
                }
                else
                {
                    string selectedCable = multiplicador + " x (" + retorno.Rows[0]["size"].ToString() + " + " + retorno.Rows[0]["grndSize"].ToString() + ")";

                    return selectedCable;
                }
            }

            else if (dadosEntrada.cableType == "Single")
            {

                int multiplicador = 1;

            volta:

                DataTable retorno = queryCables.ExecuteQuery($"Select size  from single where {aux} >= {dadosSaida.correctedCurrent / multiplicador}");

                if (retorno.Rows.Count == 0)
                {
                    multiplicador++;

                    goto volta;
                }
                else
                {
                    string selectedCable = multiplicador + " x (3 x " + retorno.Rows[0]["size"].ToString();

                    if (dadosEntrada.loadType == "Feeder")
                    {
                        string groundCable = $"select copper from grounding where amperes >= {dadosSaida.correctedCurrent}";
                        DataTable groundCableData = queryCables.ExecuteQuery(groundCable);
                        if (groundCableData.Rows.Count > 0)
                        {
                            selectedCable += " + 1 x " + groundCableData.Rows[0]["copper"].ToString() + ")";
                        }
                    }
                    else
                    {
                        string groundCable = $"select copper from grounding where amperes >= {dadosSaida.correctedCurrent / multiplicador}";
                        DataTable groundCableData = queryCables.ExecuteQuery(groundCable);
                        if (groundCableData.Rows.Count > 0)
                        {
                            selectedCable += " + 1 x " + groundCableData.Rows[0]["copper"].ToString() + ")"; ;
                        }
                    }

                    return selectedCable;
                }
            }



            else
            {
                return "";
            }
        }

        public double findCableByDropVoltage(inputData dadosEntrada, outputData dadosSaida)
        {

            if (dadosEntrada.cableType == "VFD")
            {
                return 0;
            }


            else
            {
                string tableToSearch = "";
                string columnToSearch = "";

                if (dadosEntrada.cableType == "Multicable")
                    tableToSearch = "multi";
                else
                    tableToSearch = "single";



                if (dadosEntrada.cableTempCol == 75)
                    columnToSearch = "res75";
                else
                    columnToSearch = "res90";


                string[] cableToFind = dadosSaida.powerCable.Split('x');

                string sizeToSearch = "";

                if (cableToFind.Count() > 1)
                    sizeToSearch = cableToFind[1].Trim();
                else
                    sizeToSearch = cableToFind[0].Trim();


                string query = $"Select {columnToSearch}, rea from {tableToSearch} where size = '{sizeToSearch}'";

                DataTable retorno = queryCables.ExecuteQuery(query);

                double calculatedDrop = CalcularQuedaTensao(retorno.Rows[0][columnToSearch].ToString() == "" ? 0 : Convert.ToDouble(retorno.Rows[0][columnToSearch]), retorno.Rows[0]["rea"].ToString() == "" ? 0 : Convert.ToDouble(retorno.Rows[0]["rea"]), dadosEntrada.distance, dadosSaida.correctedCurrent, dadosEntrada.powerFactor);

                return calculatedDrop;


            }
        }


        public static double CalcularQuedaTensao(double resistencia, double reatancia, double distanciaPes, double corrente, double fatorPotencia)
        {
            // Converter distância de pés para "mil pés"
            double distanciaMilPes = distanciaPes / 1000.0;

            // Calcular sin(φ)
            double senoPhi = Math.Sqrt(1 - Math.Pow(fatorPotencia, 2));

            // Aplicar fórmula trifásica
            double quedaTensao = Math.Sqrt(3) * corrente *
                                 ((resistencia * fatorPotencia) + (reatancia * senoPhi)) *
                                 distanciaMilPes;

            return quedaTensao; // resultado em volts
        }



    }






}
