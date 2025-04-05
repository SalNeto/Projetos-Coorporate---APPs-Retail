using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Oracle.ManagedDataAccess.Client;

namespace Upload_NFe_Cone_XML
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Executando pelo console do aplicativo...");
            var service = new InvoiceService();
            await service.ProcessInvoiceData(CancellationToken.None);
            await service.InsertXMLDataToDatabase(CancellationToken.None);
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }
    }

    public class InvoiceService
    {
        private CancellationTokenSource cancellationTokenSource;

        public InvoiceService()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ProcessInvoiceData(CancellationToken cancellationToken)
        {
            // Método original de processamento de dados da fatura
        }

        public async Task InsertXMLDataToDatabase(CancellationToken cancellationToken)
        {
            string connectionString = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=10.3.201.138)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=RJD14)));User ID=INTEGRA;Password=Int3gr4DUF;Connection Timeout=240;Pooling=false;";
            string inputFolder = @"C:\Temp\XML Duty Free 2019 a 2021\Notas_Emites_COne\Consolidado_Emites_Cone_TEST\Ticket CE\xml_CF-e D4024840 parte 2\2719788802484020240720184159890047\Cupons de Venda";
            string processedFolder = @"C:\Temp\XML Duty Free 2019 a 2021\Notas_Emites_COne\Consolidado_Emites_Cone_TEST\Processado";


          

            string[] xmlFiles = Directory.GetFiles(inputFolder, "*.xml");

            if (xmlFiles.Length == 0)
            {
                Console.WriteLine("Nenhum arquivo XML encontrado na pasta especificada.");
                return;
            }

            try
            {
                using (var connection = new OracleConnection(connectionString))
                {
                    Console.WriteLine("Tentando abrir a conexão com o banco de dados...");
                    await connection.OpenAsync(cancellationToken);
                    Console.WriteLine("Conexão com o banco de dados estabelecida.");

                    foreach (var xmlFile in xmlFiles)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Task has been canceled.");
                            break;
                        }

                        Console.WriteLine($"Processando arquivo: {xmlFile}");
                 
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(xmlFile);  

                        string sistemaOrigem = "CONTABILONE";

                        // Carrega o arquivo XML                                            


                        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                        nsManager.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

                        



                        // Captura do modelo do documento
                        ///string modelo = /*xmlDoc.SelectSingleNode("//ide/mod")?.InnerText ??*/ xmlDoc.SelectSingleNode("/nfeProc/NFe/infNFe/ide/mod")?.InnerText;

                        string modelo = xmlDoc.SelectSingleNode("//ide/mod")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:ide/nfe:mod", nsManager)?.InnerText;
                        string tpNF = xmlDoc.SelectSingleNode("//ide/tpNF")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:ide/nfe:tpNF", nsManager)?.InnerText;

                        string dataProcessamento = "0.00";


                        Console.WriteLine($"modelo: {modelo}");

                        

                        if (modelo == "59")
                        {
                            dataProcessamento = xmlDoc.SelectSingleNode("//ide/dEmi")?.InnerText ?? "0.00";
                        }
                        else
                        {
                            dataProcessamento = xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:ide/nfe:dhEmi", nsManager)?.InnerText;
                        }
                        
                        // Valida e formata a data se não for "0.00"
                        if (!string.IsNullOrEmpty(dataProcessamento) && dataProcessamento != "0.00")
                        {
                            try
                            {
                                DateTime parsedDate = DateTime.ParseExact(dataProcessamento, modelo == "59" ? "yyyyMMdd" : "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);
                                dataProcessamento = parsedDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                            }
                            catch (FormatException ex)
                            {
                                Console.WriteLine($"Erro ao formatar a data: {ex.Message}");
                                dataProcessamento = "0.00"; // Utilizar um fallback seguro ou lançar uma exceção conforme a necessidade do negócio
                            }
                        }

                        // Log para debug
                        Console.WriteLine($"Data de Processamento: {dataProcessamento}");

                        
                            string cnpj = xmlDoc.SelectSingleNode("//emit/CNPJ")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:emit/nfe:CNPJ", nsManager)?.InnerText;
                            string inscricaoEstadual = xmlDoc.SelectSingleNode("//emit/IE")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:emit/nfe:IE", nsManager)?.InnerText;
                            string nome = xmlDoc.SelectSingleNode("//emit/xFant")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:emit/nfe:xFant", nsManager)?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:emit/nfe:xNome", nsManager)?.InnerText;


                        string cnpj_DESTINO = xmlDoc.SelectSingleNode("//dest/CNPJ")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:dest/nfe:CNPJ", nsManager)?.InnerText;
                        string inscricaoEstadual_DESTINO = xmlDoc.SelectSingleNode("//dest/IE")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:dest/nfe:IE", nsManager)?.InnerText;
                        string nome_DESTINO = xmlDoc.SelectSingleNode("//dest/xFant")?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:dest/nfe:xFant", nsManager)?.InnerText ?? xmlDoc.SelectSingleNode("//nfe:infNFe/nfe:dest/nfe:xNome", nsManager)?.InnerText;



                        string requestId = tpNF + "|" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        string chaveAcesso;
                        if (modelo == "59")
                        {
                            chaveAcesso = xmlDoc.SelectSingleNode("//infCFe")?.Attributes["Id"]?.Value.Substring(3) ?? "0.00";
                            
                        }
                        else
                        {
                            chaveAcesso = xmlDoc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:chNFe", nsManager)?.InnerText;

                        }


                        string status;
                        if (modelo == "59")
                        {
                            status = "AUTORIZAÇÃO NORMAL";
                            Console.WriteLine($"Status: {status}");
                        }
                        else
                        {
                             status = xmlDoc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:cStat", nsManager)?.InnerText == "100"
                            ? "AUTORIZAÇÃO NORMAL"
                            : xmlDoc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:cStat", nsManager)?.InnerText + "|" + xmlDoc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:xMotivo", nsManager)?.InnerText;
                             Console.WriteLine($"Status: {status}");
                        }

                        XmlNode icmsTotNode = xmlDoc.SelectSingleNode("//total/ICMSTot"); // Incorreto para documentos com namespaces
                        XmlNode icmsTotNode55 = xmlDoc.SelectSingleNode("//nfe:total/nfe:ICMSTot", nsManager); // Correto


                        string vNF = modelo == "59"
                            ? xmlDoc.SelectSingleNode("//total/vCFe")?.InnerText ?? "0.00"
                            : icmsTotNode55?.SelectSingleNode("nfe:vNF", nsManager)?.InnerText ?? "0.00";
                        Console.WriteLine($"vNF: {vNF}");                        

                        string vBC = icmsTotNode?.SelectSingleNode("vBC")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vBC", nsManager)?.InnerText ?? "0.00";
                        string vICMS = icmsTotNode?.SelectSingleNode("vICMS")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vICMS", nsManager)?.InnerText ?? "0.00";
                                                
                        string vICMSDeson = icmsTotNode?.SelectSingleNode("vICMSDeson")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vICMSDeson", nsManager)?.InnerText ?? "0.00";
                        string vFCPUFDest = icmsTotNode?.SelectSingleNode("vFCPUFDest")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vFCPUFDest", nsManager)?.InnerText ?? "0.00";
                        string vICMSUFDest = icmsTotNode?.SelectSingleNode("vICMSUFDest")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vICMSUFDest", nsManager)?.InnerText ?? "0.00";
                        string vICMSUFRemet = icmsTotNode?.SelectSingleNode("vICMSUFRemet")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vICMSUFRemet", nsManager)?.InnerText ?? "0.00";
                        string vFCP = icmsTotNode?.SelectSingleNode("vFCP")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vFCP", nsManager)?.InnerText ?? "0.00";
                        string vBCST = icmsTotNode?.SelectSingleNode("vBCST")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vBCST", nsManager)?.InnerText ?? "0.00";
                        string vST = icmsTotNode?.SelectSingleNode("vST")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vST", nsManager)?.InnerText ?? "0.00";
                        string vFCPST = icmsTotNode?.SelectSingleNode("vFCPST")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vFCPST", nsManager)?.InnerText ?? "0.00";
                        string vFCPSTRet = icmsTotNode?.SelectSingleNode("vFCPSTRet")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vFCPSTRet", nsManager)?.InnerText ?? "0.00";
                        string vProd = icmsTotNode?.SelectSingleNode("vProd")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vProd", nsManager)?.InnerText ?? "0.00";
                        ///string vFrete = icmsTotNode?.SelectSingleNode("vFrete")?.InnerText ?? icmsTotNode55.SelectSingleNode("nfe:vFrete", nsManager)?.InnerText ?? "0.00";
                        ///
                        // Inicialize vFrete com um valor padrão primeiro
                        string vFrete = "0.00";

                        // Verifique se icmsTotNode não é nulo e tente recuperar vFrete
                        if (icmsTotNode != null)
                        {
                            var freteNode = icmsTotNode.SelectSingleNode("vFrete");
                            if (freteNode != null)
                            {
                                vFrete = freteNode.InnerText;
                            }
                        }

                        // Se vFrete ainda é "0.00" e icmsTotNode55 não é nulo, tente recuperar usando o namespace
                        if (vFrete == "0.00" && icmsTotNode55 != null)
                        {
                            var freteNodeNs = icmsTotNode55.SelectSingleNode("nfe:vFrete", nsManager);
                            if (freteNodeNs != null)
                            {
                                vFrete = freteNodeNs.InnerText;
                            }
                        }




                        string vSeg = icmsTotNode?.SelectSingleNode("vSeg")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vSeg", nsManager)?.InnerText ?? "0.00";
                        string vDesc = icmsTotNode?.SelectSingleNode("vDesc")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vDesc", nsManager)?.InnerText ?? "0.00";
                        string vII = icmsTotNode?.SelectSingleNode("vII")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vII", nsManager)?.InnerText ?? "0.00";
                        string vIPI = icmsTotNode?.SelectSingleNode("vIPI")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vIPI", nsManager)?.InnerText ?? "0.00";
                        string vIPIDevol = icmsTotNode?.SelectSingleNode("vIPIDevol")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vIPIDevol", nsManager)?.InnerText ?? "0.00";
                        string vPIS = modelo == "59"
                            ? xmlDoc.SelectSingleNode("//total/vPIS")?.InnerText ?? "0.00"
                            : icmsTotNode55?.SelectSingleNode("nfe:vPIS", nsManager)?.InnerText ?? "0.00";
                        string vCOFINS = modelo == "59"
                            ? xmlDoc.SelectSingleNode("//total/vCOFINS")?.InnerText ?? "0.00"
                            : icmsTotNode55?.SelectSingleNode("nfe:vCOFINS", nsManager)?.InnerText ?? "0.00";
                        string vOutro = icmsTotNode?.SelectSingleNode("vOutro")?.InnerText ?? icmsTotNode55?.SelectSingleNode("nfe:vOutro", nsManager)?.InnerText ?? "0.00";


                        using (var command = new OracleCommand("DELETE FROM synchro.requisicao_sefaz WHERE CHAVE_DE_ACESSO = :chaveAcesso", connection))
                        {
                            command.Parameters.Add("chaveAcesso", chaveAcesso);
                            command.ExecuteNonQuery();
                        }



                        string query = @"INSERT INTO synchro.requisicao_sefaz 
                            (SISTEMA_ORIGEM, CNPJ, INSCRIÇÃO_ESTADUAL, NOME, REQUEST_ID, REMOTE_ID, DATA_DO_PROCESSAMENTO, MODELO, CHAVE_DE_ACESSO, STATUS, VNF,CNPJ_FORNECEDOR_EMITENTE ,VBC, VICMS, VICMSDESON, VFCPUFDEST, VICMSUFDEST, VICMSUFREMET, VFCP, VBCST, VST, VFCPST, VFCPSTRET, VPROD, VFRETE, VSEG, VDESC, VII, VIPI, VIPIDEVOL, VPIS, VCOFINS, VOUTRO, STATUS_CONTINGENCIA, STATUS_AUTORIZACAO, STATUS_INUTILIZACAO, STATUS_CANCELAMENTO) 
                            VALUES 
                            (:SISTEMA_ORIGEM, :CNPJ, :INSCRIÇÃO_ESTADUAL, :NOME, :REQUEST_ID, :REMOTE_ID, :DataProcessamento, :MODELO, :CHAVE_DE_ACESSO, :STATUS, :VNF, :CNPJ_FORNECEDOR_EMITENTE ,:VBC, :VICMS, :VICMSDESON, :VFCPUFDEST, :VICMSUFDEST, :VICMSUFREMET, :VFCP, :VBCST, :VST, :VFCPST, :VFCPSTRET, :VPROD, :VFRETE, :VSEG, :VDESC, :VII, :VIPI, :VIPIDEVOL, :VPIS, :VCOFINS, :VOUTRO, :STATUS_CONTINGENCIA, :STATUS_AUTORIZACAO, :STATUS_INUTILIZACAO, :STATUS_CANCELAMENTO)";

                        using (var command = new OracleCommand(query, connection))
                        {
                            command.Parameters.Add("SISTEMA_ORIGEM", OracleDbType.Varchar2).Value = sistemaOrigem;
                            command.Parameters.Add("CNPJ", OracleDbType.Varchar2).Value = cnpj;
                            command.Parameters.Add("INSCRIÇÃO_ESTADUAL", OracleDbType.Varchar2).Value = inscricaoEstadual;
                            command.Parameters.Add("NOME", OracleDbType.Varchar2).Value = nome;
                            command.Parameters.Add("REQUEST_ID", OracleDbType.Varchar2).Value = requestId;
                            command.Parameters.Add("REMOTE_ID", OracleDbType.Varchar2).Value = requestId; // Assumindo que REMOTE_ID seja igual ao REQUEST_ID, ajuste conforme necessário
                            command.Parameters.Add("DATA_DO_PROCESSAMENTO", OracleDbType.Varchar2).Value = dataProcessamento;
                            command.Parameters.Add("MODELO", OracleDbType.Varchar2).Value = modelo;
                            command.Parameters.Add("CHAVE_DE_ACESSO", OracleDbType.Varchar2).Value = chaveAcesso;
                            command.Parameters.Add("STATUS", OracleDbType.Varchar2).Value = status;
                            command.Parameters.Add("VNF", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vNF) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vNF ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("CNPJ_FORNECEDOR_EMITENTE", OracleDbType.Varchar2).Value = cnpj_DESTINO;
                            command.Parameters.Add("VBC", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vBC) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vBC ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VICMS", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vICMS) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vICMS ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VICMSDESON", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vICMSDeson) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vICMSDeson ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VFCPUFDEST", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vFCPUFDest) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vFCPUFDest ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VICMSUFDEST", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vICMSUFDest) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vICMSUFDest ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VICMSUFREMET", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vICMSUFRemet) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vICMSUFRemet ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VFCP", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vFCP) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vFCP ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VBCST", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vBCST) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vBCST ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VST", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vST) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vST ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VFCPST", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vFCPST) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vFCPST ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VFCPSTRET", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vFCPSTRet) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vFCPSTRet ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VPROD", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vProd) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vProd ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VFRETE", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vFrete) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vFrete ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VSEG", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vSeg) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vSeg ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VDESC", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vDesc) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vDesc ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VII", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vII) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vII ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VIPI", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vIPI) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vIPI ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VIPIDEVOL", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vIPIDevol) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vIPIDevol ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VPIS", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vPIS) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vPIS ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VCOFINS", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vCOFINS) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vCOFINS ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("VOUTRO", OracleDbType.Decimal).Value = string.IsNullOrEmpty(vOutro) ? DBNull.Value : (object)decimal.Parse(Regex.Replace(vOutro ?? "0", @"[^\d.]", ""), CultureInfo.InvariantCulture);
                            command.Parameters.Add("STATUS_CONTINGENCIA", OracleDbType.Varchar2).Value = "0.00"; // Valor padrão ou ajuste conforme necessário
                            command.Parameters.Add("STATUS_AUTORIZACAO", OracleDbType.Varchar2).Value = "0.00"; // Valor padrão ou ajuste conforme necessário
                            command.Parameters.Add("STATUS_INUTILIZACAO", OracleDbType.Varchar2).Value = "0.00"; // Valor padrão ou ajuste conforme necessário
                            command.Parameters.Add("STATUS_CANCELAMENTO", OracleDbType.Varchar2).Value = "0.00"; // Valor padrão ou ajuste conforme necessário

                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }

                        // Move the processed file to the processed folder
                        string processedFilePath = Path.Combine(processedFolder, Path.GetFileName(xmlFile));

                        // Se o arquivo já existe, ele será substituído
                        File.Copy(xmlFile, processedFilePath, true);
                        File.Delete(xmlFile);

                        Console.WriteLine($"Arquivo processado e movido para: {processedFilePath}");
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
