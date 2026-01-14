using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Reportes.Estilos
{
    public class EstiloReporte
    {
        public static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.ExtraBold())
                .Background(Colors.Blue.Darken4)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(Colors.White))
                .BorderColor(Colors.Grey.Lighten1);
        }
        public static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .PaddingVertical(1)
                .PaddingHorizontal(3)
                .BorderColor(Colors.Grey.Lighten3);
        }
    }
}