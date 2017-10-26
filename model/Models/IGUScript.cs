using System;
using System.Collections.Generic;

using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class IGUScript : INameable, IScriptable, ICustomName {

        public const string BASE_PATH = "funcionalidades\\ConfiguracionIGU";
        public const string FILENAME_SUFFIX = "_IGU";

        public string Modulo { get; set; }
		public string Name { get; set; }

        public string Subdir { get; set; }
        public string Filename { get; set; }

        public String scriptFrm;

		public List<string> lineasGrid = new List<string>();
		public List<string> lineasColumnasGrid = new List<string>();
		public List<string> lineasDropdownGrid = new List<string>();
        public List<string> lineasColumnasCheckMensaje = new List<string>();

        public DateTime? ModifyDate { get; set; }


		public IGUScript(SqlDataReader dr)
		{
            Modulo = (string)dr["Modulo"];
			Name = (string)dr["Nombre"];

            Subdir = Modulo;
            Filename = Name + FILENAME_SUFFIX;


            StringBuilder sb = new StringBuilder("\tEXEC spInsFormulario ");

			sb.Append(gcs(dr["Modulo"]));
			sb.Append(gcs(dr["Nombre"]));
			sb.Append(gcs(dr["Descripcion"], ""));

			scriptFrm = sb.ToString();
		}


        public static void LoadIGUScripts(SqlCommand cm, List<IGUScript> IguScripts, Action<TraceLevel, string> log)
        {
            try
            {
                // get synonyms
                cm.CommandText = @"
						select *
						from _vwFormularios
                        where Activado = 1
                ";
                using (var dr = cm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        IGUScript iguScript = new IGUScript(dr);

                        IguScripts.Add(iguScript);
                    }
                }
            }
            catch (Exception e)
            {
                log(TraceLevel.Error, e.Message);
            }

            try
            {
                // get synonyms
                cm.CommandText = @"
						select *
						from _vwGrids
						order by 1";
                using (var dr = cm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        IGUScript iguScript = IguScripts.FirstOrDefault(f => f.Name == ((string)dr["NombreFormulario"]));

                        if (iguScript != null)
                        {
                            iguScript.addLineaGrid(dr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log(TraceLevel.Error, e.Message);
            }

            try
            {
                // get synonyms
                cm.CommandText = @"
						select *
						from _vwColumnasGrid v
						order by v.IDFormulario, v.IDGrid, v.Secuencia";
                using (var dr = cm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        IGUScript iguScript = IguScripts.FirstOrDefault(f => f.Name == ((string)dr["NombreFormulario"]));

                        if (iguScript != null)
                        {
                            iguScript.addLineaColumnaGrid(dr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log(TraceLevel.Error, e.Message);
            }

            try
            {
                // get synonyms
                cm.CommandText = @"
						select *
						from _vwDropdownsGrid v
						order by v.Formulario, v.kDropdown";
                using (var dr = cm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        IGUScript iguScript = IguScripts.FirstOrDefault(f => f.Name == ((string)dr["Formulario"]));

                        if (iguScript != null)
                        {
                            iguScript.addLineaDropdownGrid(dr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log(TraceLevel.Error, e.Message);
            }

            try
            {
                // get synonyms
                cm.CommandText = @"
						select *
						from _vwIGUColumnasCheckMensaje v
						order by v.NombreFormulario, v.IDGrid, v.Columna";
                using (var dr = cm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        IGUScript iguScript = IguScripts.FirstOrDefault(f => f.Name == ((string)dr["NombreFormulario"]));

                        if (iguScript != null)
                        {
                            iguScript.addLineaColumnaCheckMensaje(dr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log(TraceLevel.Error, e.Message);
            }
        }






        public void addLineaGrid(SqlDataReader dr)
		{
			StringBuilder sb = new StringBuilder("\tEXEC spInsGrid ");

			sb.Append(gcs(dr["Modulo"]));
			sb.Append(gcs(dr["NombreFormulario"]));
			sb.Append(gcs(dr["NombreGrid"], anchoCampoScript: 13));
			sb.Append(gcs(dr["DataMember"], anchoCampoScript: 30));
			sb.Append(gcs(dr["IDGridMaestro"], anchoCampoScript: 5));
			sb.Append(gcs(dr["TablasBase"], anchoCampoScript: 30));
			sb.Append(gcs(dr["ColumnaFocoDefecto"], anchoCampoScript: 20));
			sb.Append(gcs(dr["SplitFocoDefecto"], anchoCampoScript: 5));
			sb.Append(gcs(dr["OrderByDefecto"], anchoCampoScript: 30));
			sb.Append(gcs(dr["OrderByAlta"], anchoCampoScript: 20));
			sb.Append(gcs(dr["FiltroDefecto"], anchoCampoScript: 25));
			sb.Append(gcs(dr["CamposRSRelacion"], anchoCampoScript: 30));
			sb.Append(gcs(dr["ColumnasGridMaestroRelacion"], anchoCampoScript: 30));
			sb.Append(gcs(dr["VistaDatosDefecto"], anchoCampoScript: 5));
			sb.Append(gcs(dr["DropdownsAutomaticos"], anchoCampoScript: 5));
			sb.Append(gcs(dr["NomRSDatos"], anchoCampoScript: 12));
			sb.Append(gcs(dr["CargarDatos"], anchoCampoScript: 5));
			sb.Append(gcs(dr["GenerarColumnasAuto"], anchoCampoScript: 5));
			sb.Append(gcs(dr["TituloGrid"], anchoCampoScript: 35));
			sb.Append(gcs(dr["CamposClave"], ""));

			lineasGrid.Add(sb.ToString());
		}

		public void addLineaColumnaGrid(SqlDataReader dr)
		{
			StringBuilder sb = new StringBuilder("\tEXEC spInsColumnaGrid ");

			sb.Append(gcs(dr["Modulo"]));
			sb.Append(gcs(dr["NombreFormulario"]));
			sb.Append(gcs(dr["NombreGrid"]));
			sb.Append(gcs(DBNull.Value));
			sb.Append(gcs(dr["Columna"], anchoCampoScript: 20));
			sb.Append(gcs(dr["Titulo"], anchoCampoScript: 30));
			sb.Append(gcs(dr["Alineacion"], anchoCampoScript: 3));
			sb.Append(gcs(dr["Tamaño"], anchoCampoScript: 4));
			sb.Append(gcs(dr["Visible"], anchoCampoScript: 2));
			sb.Append(gcs(dr["OpSumarizacion"], anchoCampoScript: 2));
			sb.Append(gcs(dr["CamposOp"], anchoCampoScript: 3));
			sb.Append(gcs(dr["Requerida"], anchoCampoScript: 2));
			sb.Append(gcs(dr["FormatText"], anchoCampoScript: 2));
			sb.Append(gcs(dr["CadLineaEstado"], ""));

			lineasColumnasGrid.Add(sb.ToString());
		}

		public void addLineaDropdownGrid(SqlDataReader dr)
		{
			StringBuilder sb = new StringBuilder("\tEXEC spInsDropdownGrid ");

			sb.Append(gcs(dr["Modulo"]));
			sb.Append(gcs(dr["Formulario"]));
			sb.Append(gcs(dr["Grid"], anchoCampoScript: 15));
			sb.Append(gcs(dr["NombreDropdown"], anchoCampoScript: 25));
			sb.Append(gcs(dr["Multiple"], anchoCampoScript: 2));
			sb.Append(gcs(dr["Datafield"], anchoCampoScript: 20));
			sb.Append(gcs(dr["ListFields"], anchoCampoScript: 50));
			sb.Append(gcs(dr["ColumnaGridClaveRef"], anchoCampoScript: 20));
			sb.Append(gcs(dr["ColumnasGrid"], anchoCampoScript: 50));
			sb.Append(gcs(dr["DataMember"], anchoCampoScript: 50));
			sb.Append(gcs(dr["OrderBy"], anchoCampoScript: 50));
			sb.Append(gcs(dr["FiltroDefecto"], anchoCampoScript: 20));
			sb.Append(gcs(dr["CamposRSRelacion"]));
			sb.Append(gcs(dr["ColumnasGridFiltro"], "", anchoCampoScript: 20));

			lineasDropdownGrid.Add(sb.ToString());
		}

        public void addLineaColumnaCheckMensaje(SqlDataReader dr)
        {
            StringBuilder sb = new StringBuilder("\tEXEC _spIGUColumnaCheckMensaje ");

            sb.Append(gcs(dr["Modulo"]));
            sb.Append(gcs(dr["NombreFormulario"]));
            sb.Append(gcs(dr["NombreGrid"], anchoCampoScript: 15));
            sb.Append(gcs(dr["Columna"], anchoCampoScript: 25));
            sb.Append(gcs(dr["NomVista"], anchoCampoScript: 50));
            sb.Append(gcs(dr["CampoClaveVista"], anchoCampoScript: 25));
            sb.Append(gcs(dr["CampoMensajeVista"], anchoCampoScript: 25));
            sb.Append(gcs(dr["CampoRelVistaGrid"], "", anchoCampoScript: 25));
            if (! dr["ColorRGB"].Equals(DBNull.Value))
                sb.Append(gcs(dr["ColorRGB"], "", anchoCampoScript: 15, dc: "", carAnterior: ", "));

            lineasColumnasCheckMensaje.Add(sb.ToString());
        }


        public string ScriptCreate()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("begin");
			sb.AppendLine("\tset nocount on");
			sb.AppendLine("\tbegin transaction tIGU");
			sb.AppendLine();
			sb.AppendLine(scriptFrm);
			sb.AppendLine();
			foreach (string linea in lineasGrid)
			{
				sb.AppendLine(linea);
			}
			sb.AppendLine();
			foreach (string linea in lineasColumnasGrid)
			{
				sb.AppendLine(linea);
			}
			sb.AppendLine();
			foreach (string linea in lineasDropdownGrid)
			{
				sb.AppendLine(linea);
			}
            sb.AppendLine();
            foreach (string linea in lineasColumnasCheckMensaje)
            {
                sb.AppendLine(linea);
            }
            sb.AppendLine();

            sb.AppendLine($"\tselect * from _vwColumnasGrid v where v.NombreFormulario = '{Name}' order by v.IDGrid, v.Secuencia");
            if (lineasDropdownGrid.Count > 0)
            {
                sb.AppendLine($"\tselect * from _vwDropdownsGrid v where v.Formulario = '{Name}' order by v.IDGrid, v.NombreDropdown");
            }
            if (lineasColumnasCheckMensaje.Count > 0)
            {
                sb.AppendLine($"\tselect * from _vwIGUColumnasCheckMensaje v where v.NombreFormulario = '{Name}' order by v.IDGrid, SecuenciaColumna");
            }

            sb.AppendLine();
			sb.AppendLine("\tcommit transaction tIGU");
			sb.AppendLine("end");

			return sb.ToString();
		}


		private const string DC = "'";
		//private const string DC2 = DC + DC;
		private string gcs(Object o, string carPosterior = ", ", bool valorNullCad = false, int anchoCampoScript = 0, string dc = DC, string carAnterior = "")
		{
			String cad = "";
            string dc2 = dc + dc;

			if (o.Equals(DBNull.Value))
			{
				cad = "NULL";
			}
			else if (o is String)
			{
				string v = (string)o;
				if ((v.Trim() == "") && (valorNullCad == true))
				{
					cad = "NULL";
				}
				else if (dc == "")
                {
                    cad = v;
                }
                else
				{
					cad = dc + v.Replace(dc, dc2) + dc;
				}
			}
			else if (o is int)
			{
				cad = ((int)o).ToString();
			}
			else if (o is bool)
			{
				cad = ((bool)o) ? "1" : "0";
			}
			else
				cad = "**ERROR**";

			if ((anchoCampoScript > 0) && (anchoCampoScript > cad.Length))
			{
				cad = cad + new string(' ', anchoCampoScript - cad.Length);
			}

			cad = carAnterior + cad + carPosterior;

			return cad;
		}

	}
}
