using System.Xml;
using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Text;

class ConvertidorXMLaCSV
{
    static void Main(string[] args)
    {
        Console.WriteLine("Por favor, ingresa la ruta de la carpeta donde se encuentran los archivos XML:");
        string ruta = Console.ReadLine();

        if (!Directory.Exists(ruta))
        {
            Console.WriteLine("La ruta proporcionada no es válida.");
            return;
        }

        string[] xmlFiles = Directory.GetFiles(ruta, "*.xml");

        if (xmlFiles.Length < 1)
        {
            Console.WriteLine("Se necesitan al menos 1 archivo XML en la carpeta proporcionada.");
            return;
        }

        string outputCsv = Path.Combine(ruta, "resultado.csv");

        using (StreamWriter writer = new StreamWriter(outputCsv))
        {
            writer.WriteLine("EndToEndId;MndtId;Ustrd");

            foreach (var xmlFile in xmlFiles)
            {
                Console.WriteLine($"Procesando el archivo: {xmlFile}");
                try
                {
                    writer.WriteLine($"--- Resultados del archivo: {Path.GetFileName(xmlFile)} ---");
                    ProcesarXML(xmlFile, writer);
                    writer.WriteLine(""); // Añadir una línea en blanco para separación visual
                }
                catch (XmlException xmlEx)
                {
                    Console.WriteLine($"Error de XML al procesar el archivo {xmlFile}: {xmlEx.Message}");
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"Error de entrada/salida al procesar el archivo {xmlFile}: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inesperado al procesar el archivo {xmlFile}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"El archivo CSV se ha generado como: {outputCsv}");
        Console.ReadLine();
    }

    static void ProcesarXML(string xmlFile, StreamWriter writer)
    {
        try
        {
            // Usar StreamReader con la codificación correcta
            using (var reader = new StreamReader(xmlFile, Encoding.UTF8)) // Puedes usar Encoding.Default o Encoding.Unicode según sea necesario
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ns", "urn:iso:std:iso:20022:tech:xsd:camt.054.001.02");

                // Obtener todos los nodos 'Ntry' directamente
                XmlNodeList ntryList = doc.SelectNodes("//ns:Ntry", nsmgr);

                if (ntryList.Count == 0)
                {
                    Console.WriteLine($"No se encontraron nodos 'Ntry' en el archivo {xmlFile}.");
                    return;
                }

                foreach (XmlNode ntry in ntryList)
                {
                    string endToEndId = GetNodeText(ntry, "ns:NtryDtls/ns:TxDtls/ns:Refs/ns:EndToEndId", nsmgr);
                    string mndtId = GetNodeText(ntry, "ns:NtryDtls/ns:TxDtls/ns:Refs/ns:MndtId", nsmgr);
                    string ustrd = NormalizeUstrd(GetNodeText(ntry, "ns:NtryDtls/ns:TxDtls/ns:RmtInf/ns:Ustrd", nsmgr));

                    Console.WriteLine($"Procesando Ntry: EndToEndId={endToEndId}, MndtId={mndtId}, Ustrd={ustrd}");

                    if (string.IsNullOrEmpty(endToEndId) && string.IsNullOrEmpty(mndtId) && string.IsNullOrEmpty(ustrd))
                    {
                        Console.WriteLine($"El nodo 'Ntry' no tiene datos válidos en el archivo {xmlFile}.");
                        continue;
                    }

                    writer.WriteLine($"{endToEndId};{mndtId};{ustrd}");
                }
            }
        }
        catch (XmlException xmlEx)
        {
            Console.WriteLine($"Error al procesar el XML de {xmlFile}: {xmlEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Se produjo un error al procesar el archivo {xmlFile}: {ex.Message}");
        }
    }

    static string GetNodeText(XmlNode parent, string path, XmlNamespaceManager nsmgr)
    {
        XmlNode node = parent.SelectSingleNode(path, nsmgr);
        return node != null ? node.InnerText.Trim() : string.Empty;
    }

    static string NormalizeUstrd(string ustrd)
    {
        if (string.IsNullOrEmpty(ustrd)) return string.Empty;
        ustrd = Regex.Replace(ustrd, @"\s+", " ").Trim();
        return ustrd;
    }
}
