namespace Parking.Business.EmailTemplates
{
    public interface IEmailTemplate
    {
        string To { get; }
        
        string Subject { get; }
        
        string PlainTextBody { get; }
        
        string HtmlBody { get; }
    }
}