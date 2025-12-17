public class Utils
{
    public string NumeroALiteral(decimal numero, string moneda = "BOLIVIANOS")
    {
        long parteEntera = (long)Math.Floor(numero);
        int centavos = (int)((numero - parteEntera) * 100);

        return $"SON: ({ConvertirNumero(parteEntera).ToUpper()} {centavos:00}/100)";
    }
    private static string ConvertirNumero(long numero)
    {
        if (numero == 0) return "cero";

        if (numero < 0)
            return "menos " + ConvertirNumero(Math.Abs(numero));

        string[] unidades = {
            "", "uno", "dos", "tres", "cuatro", "cinco",
            "seis", "siete", "ocho", "nueve", "diez",
            "once", "doce", "trece", "catorce", "quince"
        };

        string[] decenas = {
            "", "diez", "veinte", "treinta", "cuarenta",
            "cincuenta", "sesenta", "setenta", "ochenta", "noventa"
        };

        string[] centenas = {
            "", "ciento", "doscientos", "trescientos", "cuatrocientos",
            "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"
        };

        if (numero < 16)
            return unidades[numero];

        if (numero < 20)
            return "dieci" + unidades[numero - 10];

        if (numero < 30)
            return numero == 20 ? "veinte" : "veinti" + unidades[numero - 20];

        if (numero < 100)
            return decenas[numero / 10] + (numero % 10 > 0 ? " y " + unidades[numero % 10] : "");

        if (numero == 100)
            return "cien";

        if (numero < 1000)
            return centenas[numero / 100] + (numero % 100 > 0 ? " " + ConvertirNumero(numero % 100) : "");

        if (numero < 1000000)
            return ConvertirNumero(numero / 1000) + " mil" +
                (numero % 1000 > 0 ? " " + ConvertirNumero(numero % 1000) : "");

        if (numero < 1000000000)
            return ConvertirNumero(numero / 1000000) + " millones" +
                (numero % 1000000 > 0 ? " " + ConvertirNumero(numero % 1000000) : "");

        return "";
    }
}

