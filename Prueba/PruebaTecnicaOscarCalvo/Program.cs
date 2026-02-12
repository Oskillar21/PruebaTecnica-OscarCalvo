using System;
using System.IO;
using System.Globalization;

//------------------------------------
// Obtener el "InputData.txt".
//------------------------------------
var basePath = AppContext.BaseDirectory;
var filePath = Path.Combine(basePath, "Incoming", "InputData.txt");
var lines = File.ReadAllLines(filePath);

//----------------------------------------------------------------------
// Obtener la linea, dividirla y crear su formato para el cvs
//----------------------------------------------------------------------
var structuresList = new List<string>();
decimal totalHeaderAmount = 0;
int i = 1;
while (i != lines.Length)
{ 

    var parts = lines[i].Split('|');

    // Creacion de la estructura del cliente
    var customerStructure = LineFormater(

        "CUSTOMER_RECORD",
        parts[0], 
        parts[1],
        parts[2],
        parts[4],
        parts[3],
        parts[5],
        parts[6],
        parts[7],
        parts[8],
        parts[9]

    );

    structuresList.Add(customerStructure);
    decimal totalDetailAmount = 0;

    // For para recorrer los distintos detalles del cliente, dinamico.
    for (int x = 10; x < parts.Length; x = x + 2)
    {
        decimal value = decimal.Parse(parts[x + 1], CultureInfo.InvariantCulture);
        totalDetailAmount += value;

        // Creacion de la estructura del detalle del cliente
        var detailStructure = LineFormater(

            "DETAILS_RECORD",
            parts[x],
            CodeFinder(value),
            MoneyFormater(value)
        );

        structuresList.Add(detailStructure);

    }

    // Creacion de la estructura del detalle "final" del cliente
    var lastDetailStructure = LineFormater(

            "DETAILS_RECORD",
            "TOTAL",
            MoneyFormater(totalDetailAmount)   
    );

    structuresList.Add(lastDetailStructure);
    totalHeaderAmount = totalHeaderAmount + totalDetailAmount;

    i++;
}


//--------------------------------------------------
// Creacion de la estructura del Header
//--------------------------------------------------
var incomingDataName = Path.GetFileName(filePath);
int customersCout = lines.Length - 1;
var todayDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
var todayTime = DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture);

var headerStructure = LineFormater(
    "HEADER_RECORD",
    incomingDataName,
    customersCout.ToString(),
    MoneyFormater(totalHeaderAmount),
    todayDate,
    todayTime
);

structuresList.Insert(0, headerStructure);

//--------------------------------------------------
// Metodo para crear las estructuras
//--------------------------------------------------
static string LineFormater(params string[] values)
{
    var sb = new System.Text.StringBuilder();

    for (int i = 0; i < values.Length; i++)
    {
        var escaped = values[i].Replace("\"", "\"\"");
        sb.Append('"').Append(escaped).Append('"');

        if (i < values.Length - 1)
            sb.Append(',');
    }

    return sb.ToString();
}


//---------------------------------------------------------
// Metodo para obtener el respectivo "code" segun su monto
//---------------------------------------------------------
static string CodeFinder(decimal amount)
{
    if (amount < 500) return "N";
    if (amount < 1000) return "A";
    if (amount < 1500) return "C";
    if (amount < 2000) return "L";
    if (amount < 2500) return "P";
    if (amount < 3000) return "X";
    if (amount < 5000) return "T";
    if (amount < 10000) return "S";
    if (amount < 20000) return "U";
    if (amount < 30000) return "R";
    if (amount >= 30000) return "V";

    return "No existe un codigo asociado a ese monto.";
}

//---------------------------------------------------------
// Metodo para el formato de los montos y totales
//---------------------------------------------------------
static string MoneyFormater(decimal amount)
{
    return $"${amount.ToString("#,##0.00", CultureInfo.InvariantCulture)}";
}


//--------------------------------------------------
// Creacion del OutputData y realizar Backups!
//--------------------------------------------------

// Obtener la ruta del proyecto
var projectRoot = Path.GetFullPath(
    Path.Combine(basePath, "..", "..", "..") //NOTA: Utilizo esta ruta para crear la carpeta adentro de la solucion y asi no la tengas que buscar por aparte! 
);

// Crear la carpeta "Outgoing"
var outgoingDir = Path.Combine(projectRoot, "Outgoing");
Directory.CreateDirectory(outgoingDir);

// Crear el archivo "OutputData.txt" con la lista de las estructuras
var outputFilePath = Path.Combine(outgoingDir, "OutputData.txt");
File.WriteAllLines(outputFilePath, structuresList);

// Crear la carpeta "Backup"
var backupDir = Path.Combine(projectRoot, "Backup");
Directory.CreateDirectory(backupDir);

// TimeStap para diferenciar los backups y tener registro de su creacion. 
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); //

// Obtener la ubicacion de los archivos
var originalInput = Path.Combine(projectRoot, "Incoming", "InputData.txt");
var originalOutput = Path.Combine(projectRoot, "Outgoing", "OutputData.txt");

// Crear los backups
var backupInput = Path.Combine(backupDir, $"InputData_{timestamp}.txt");
var backupOutput = Path.Combine(backupDir, $"OutputData_{timestamp}.txt");

// Copiarlos en su respectiva carpeta
File.Copy(originalInput, backupInput, true);
File.Copy(originalOutput, backupOutput, true);
