using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DawaDotnetClient2
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Historik
    {
        public DateTime oprettet { get; set; }
        public DateTime ændret { get; set; }
        public DateTime ikrafttrædelse { get; set; }
        public object nedlagt { get; set; }
    }

    public class Vejstykke
    {
        public string href { get; set; }
        public string navn { get; set; }
        public string adresseringsnavn { get; set; }
        public string kode { get; set; }
    }

    public class Navngivenvej
    {
        public string href { get; set; }
        public string id { get; set; }
    }

    public class Postnummer
    {
        public string href { get; set; }
        public string nr { get; set; }
        public string navn { get; set; }
    }

    public class Kommune
    {
        public string href { get; set; }
        public string kode { get; set; }
        public string navn { get; set; }
    }

    public class Ejerlav
    {
        public int kode { get; set; }
        public string navn { get; set; }
        public string href { get; set; }
    }

    public class Adgangspunkt
    {
        public string id { get; set; }
        public List<double> koordinater { get; set; }
        public double højde { get; set; }
        public string nøjagtighed { get; set; }
        public int kilde { get; set; }
        public string tekniskstandard { get; set; }
        public double tekstretning { get; set; }
        public DateTime ændret { get; set; }
    }

    public class Vejpunkt
    {
        public string id { get; set; }
        public string kilde { get; set; }
        public string nøjagtighed { get; set; }
        public string tekniskstandard { get; set; }
        public List<double> koordinater { get; set; }
        public DateTime ændret { get; set; }
    }

    public class DDKN
    {
        public string m100 { get; set; }
        public string km1 { get; set; }
        public string km10 { get; set; }
    }

    public class Sogn
    {
        public string href { get; set; }
        public string kode { get; set; }
        public string navn { get; set; }
    }

    public class Region
    {
        public string href { get; set; }
        public string kode { get; set; }
        public string navn { get; set; }
    }

    public class Landsdel
    {
        public string href { get; set; }
        public string nuts3 { get; set; }
        public string navn { get; set; }
    }

    public class Retskreds
    {
        public string href { get; set; }
        public string kode { get; set; }
        public string navn { get; set; }
    }

    public class Politikreds
    {
        public string href { get; set; }
        public string kode { get; set; }
        public string navn { get; set; }
    }

    public class Opstillingskreds
    {
        public string href { get; set; }
        public string kode { get; set; }
        public string navn { get; set; }
    }

    public class Afstemningsområde
    {
        public string href { get; set; }
        public string nummer { get; set; }
        public string navn { get; set; }
    }

    public class Storkreds
    {
        public string href { get; set; }
        public string nummer { get; set; }
        public string navn { get; set; }
    }

    public class Valglandsdel
    {
        public string href { get; set; }
        public string bogstav { get; set; }
        public string navn { get; set; }
    }

    public class Jordstykke
    {
        public string href { get; set; }
        public Ejerlav ejerlav { get; set; }
        public string matrikelnr { get; set; }
        public string esrejendomsnr { get; set; }
    }

    public class Bebyggelser
    {
        public string id { get; set; }
        public int? kode { get; set; }
        public string type { get; set; }
        public string navn { get; set; }
        public string href { get; set; }
    }

    public class Adgangsadresse
    {
        public string href { get; set; }
        public string id { get; set; }
        public string adressebetegnelse { get; set; }
        public string kvh { get; set; }
        public int status { get; set; }
        public int darstatus { get; set; }
        public Vejstykke vejstykke { get; set; }
        public string husnr { get; set; }
        public Navngivenvej navngivenvej { get; set; }
        public object supplerendebynavn { get; set; }
        public object supplerendebynavn2 { get; set; }
        public Postnummer postnummer { get; set; }
        public object stormodtagerpostnummer { get; set; }
        public Kommune kommune { get; set; }
        public Ejerlav ejerlav { get; set; }
        public string esrejendomsnr { get; set; }
        public string matrikelnr { get; set; }
        public Historik historik { get; set; }
        public Adgangspunkt adgangspunkt { get; set; }
        public Vejpunkt vejpunkt { get; set; }
        public DDKN DDKN { get; set; }
        public Sogn sogn { get; set; }
        public Region region { get; set; }
        public Landsdel landsdel { get; set; }
        public Retskreds retskreds { get; set; }
        public Politikreds politikreds { get; set; }
        public Opstillingskreds opstillingskreds { get; set; }
        public Afstemningsområde afstemningsområde { get; set; }
        public Storkreds storkreds { get; set; }
        public Valglandsdel valglandsdel { get; set; }
        public string zone { get; set; }
        public Jordstykke jordstykke { get; set; }
        public List<Bebyggelser> bebyggelser { get; set; }
        public bool brofast { get; set; }
    }

    public class Root
    {
        public string id { get; set; }
        public string kvhx { get; set; }
        public int status { get; set; }
        public int darstatus { get; set; }
        public string href { get; set; }
        public Historik historik { get; set; }
        public string etage { get; set; }
        public string dør { get; set; }
        public string adressebetegnelse { get; set; }
        public Adgangsadresse adgangsadresse { get; set; }
    }


}
