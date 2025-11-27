namespace ApiGuardian.Application.Interfaces;

public interface ILogService
{
    void Info(string id, string archivo, string metodo,  string message);
    void Warning(string id, string archivo, string metodo,  string message);
    void Error(string id, string archivo, string metodo,  string message, Exception? ex = null) ;
}
