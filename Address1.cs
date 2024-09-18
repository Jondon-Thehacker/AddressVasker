using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DawaDotnetClient1
{

    public class Adresse
    {
        public string id { get; set; }
        public string vejnavn { get; set; }
        public string adresseringsvejnavn { get; set; }
        public string husnr { get; set; }
        public object supplerendebynavn { get; set; }
        public string postnr { get; set; }
        public string postnrnavn { get; set; }
        public int status { get; set; }
        public DateTime virkningstart { get; set; }
        public object virkningslut { get; set; }
        public string adgangsadresseid { get; set; }
        public string etage { get; set; }
        public string dør { get; set; }
        public string href { get; set; }
    }

    public class Aktueladresse
    {
        public string id { get; set; }
        public string vejnavn { get; set; }
        public string adresseringsvejnavn { get; set; }
        public string husnr { get; set; }
        public object supplerendebynavn { get; set; }
        public string postnr { get; set; }
        public string postnrnavn { get; set; }
        public string status { get; set; }
        public DateTime virkningstart { get; set; }
        public object virkningslut { get; set; }
        public string adgangsadresseid { get; set; }
        public string etage { get; set; }
        public string dør { get; set; }
        public string href { get; set; }

        public static bool checkStatusNull(Aktueladresse adr)
        {
            return adr.Equals(null);
        }
    }

    public class Variant
    {
        public string vejnavn { get; set; }
        public string husnr { get; set; }
        public string etage { get; set; }
        public string dør { get; set; }
        public object supplerendebynavn { get; set; }
        public string postnr { get; set; }
        public string postnrnavn { get; set; }
    }

    public class Forskelle
    {
        public int vejnavn { get; set; }
        public int husnr { get; set; }
        public int postnr { get; set; }
        public int postnrnavn { get; set; }
        public int etage { get; set; }
        public int dør { get; set; }
    }

    public class Parsetadresse
    {
        public string vejnavn { get; set; }
        public string husnr { get; set; }
        public string etage { get; set; }
        public string dør { get; set; }
        public string postnr { get; set; }
        public string postnrnavn { get; set; }
    }

    public class Vaskeresultat
    {
        public Variant variant { get; set; }
        public int afstand { get; set; }
        public Forskelle forskelle { get; set; }
        public Parsetadresse parsetadresse { get; set; }
        public List<object> ukendtetokens { get; set; }
        public object anvendtstormodtagerpostnummer { get; set; }
    }

    public class Resultater
    {
        public Adresse adresse { get; set; }
        public Aktueladresse aktueladresse { get; set; }
        public Vaskeresultat vaskeresultat { get; set; }
    }

    public class Root
    {
        public string kategori { get; set; }
        public List<Resultater> resultater { get; set; }
    }


}

