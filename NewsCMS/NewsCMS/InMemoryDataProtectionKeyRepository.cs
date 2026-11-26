using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;

namespace NewsCMS.Services.Chatbot;

/// <summary>Volatile key storage for isolated Development chatbot verification.</summary>
internal sealed class InMemoryDataProtectionKeyRepository : IXmlRepository
{
    private readonly object _sync = new();
    private readonly List<XElement> _elements = new();

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        lock (_sync)
        {
            return _elements.Select(element => new XElement(element)).ToArray();
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        lock (_sync)
        {
            _elements.Add(new XElement(element));
        }
    }
}
