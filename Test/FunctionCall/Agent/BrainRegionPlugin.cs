using System.ComponentModel;
using System.Data;
using Microsoft.SemanticKernel;
using OfficeOpenXml;

namespace FunctionCall.Agent;

public class BrainRegionPlugin
{
    [KernelFunction]
    [Description("获取指定SWC神经元的所属脑区以及是鼠脑还是人脑。")]
    public async Task<string> GetCurrentTemperature(
        Kernel kernel,
        [Description("请输入一个SWC神经元的ID")] string id
    )
    {
        var mouseBrains = Directory.GetFiles("D:\\Temp\\Data\\BrainRegion\\result");
        var humanBrains = Directory.GetFiles("D:\\Temp\\Data\\HumanBrainRegion\\swc");

        var mouseBrain = mouseBrains.FirstOrDefault(x => Path.GetFileName(x).Contains(id));
        var humanBrain = humanBrains.FirstOrDefault(x => Path.GetFileName(x).Contains(id));
        if (mouseBrain != null)
        {
            var brainRegion = GetBrainRegion(id);
            return "这个神经元是鼠脑的，所属脑区是" + brainRegion + "。";
        }
        else if (humanBrain != null)
        {
            var brainRegion = findBrainRegion(id);
            return "这个神经元是人脑的，所属脑区是" + brainRegion + "。";
        }
        else
        {
            return "抱歉，我不知道这个神经元所属的脑区以及它是鼠脑还是人脑的神经元，因为数据库中没有找到这个神经元。";
        }
    }


    static DataTable LoadExcelFile(string path)
    {
        // Set the license context for EPPlus
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using (var package = new ExcelPackage(new FileInfo(path)))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            DataTable dataTable = new DataTable();

            foreach (var header in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                dataTable.Columns.Add(header.Text);
            }

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var newRow = dataTable.NewRow();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    newRow[col - 1] = worksheet.Cells[row, col].Text;
                }

                dataTable.Rows.Add(newRow);
            }

            return dataTable;
        }
    }

    static string findBrainRegion(string brainID)
    {
        string excelFilePath = @"D:\Temp\Data\HumanBrainRegion\samplesheet.xlsx";
        string folderPath = @"D:\Temp\Data\HumanBrainRegion\swc";
        var brainRegionCount = new Dictionary<string, int>();

// Load the Excel file into a DataTable
        DataTable dataTable = LoadExcelFile(excelFilePath);

        foreach (var filePath in Directory.GetFiles(folderPath, "*.eswc"))
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".eswc") && fileName.Contains(brainID))
            {
                string patientNumber = fileName.Split('_')[1];

                // Check if a matching record is found
                var matchingRows = dataTable.AsEnumerable()
                    .Where(row => row.Field<string>("patient_number") == patientNumber);

                if (matchingRows.Any())
                {
                    string brainRegion = matchingRows.First().Field<string>("english_full_name");
                    if (brainRegionCount.ContainsKey(brainRegion))
                    {
                        brainRegionCount[brainRegion]++;
                    }
                    else
                    {
                        brainRegionCount[brainRegion] = 1;
                    }

                    //Console.WriteLine($"Patient number {patientNumber} has brain region {brainRegion}");
                    return brainRegion;
                }
                else
                {
                    //Console.WriteLine($"Warning: No matching record found for patient number {patientNumber}");
                    File.Delete(filePath);
                    return "Unknown";
                }
            }
        }

        return "数据库中没有找到这个神经元。";
    }

    static string GetMostFrequentRegion(string filePath)
    {
        var regionCounts = new Dictionary<string, int>();

        using (var reader = new StreamReader(filePath))
        {
            string headerLine = reader.ReadLine(); // Assuming first line is header
            string[] headers = headerLine.Split(',');
            int regionIndex = Array.IndexOf(headers, "region");

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                string region = values[regionIndex];

                if (regionCounts.ContainsKey(region))
                    regionCounts[region]++;
                else
                    regionCounts[region] = 1;
            }
        }

        return regionCounts.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    static string GetBrainRegion(string id)
    {
        string csvDirectory = @"D:\Temp\Data\BrainRegion\result";
        string jsonFilePath = @"D:\Temp\Data\RegionCode\tree.json";
        var csvFiles = Directory.GetFiles(csvDirectory, "*.csv");

        foreach (var file in csvFiles)
        {
            if (Path.GetFileName(file).Contains(id))
            {
                string mostFrequentRegion = GetMostFrequentRegion(file);
                //Console.WriteLine($"Most frequent region in {Path.GetFileName(file)}: {mostFrequentRegion}");
                return mostFrequentRegion;
            }
        }

        return "数据库中没有找到这个神经元。";
    }
}