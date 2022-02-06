using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BSU.Core.Launch.BiFileTypes;

[XmlRoot(ElementName = "addons-presets")]
public class Preset2
{
    [XmlElement("last-update")]
    public DateTime LastUpdated;

    [XmlArray("published-ids")]
    [XmlArrayItem("id")]
    public List<string> PublishedId = new();

    [XmlArray("dlcs-appids")]
    [XmlArrayItem("id")]
    public List<string> DlcIds = new();
}
