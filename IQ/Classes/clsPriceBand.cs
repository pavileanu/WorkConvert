/// <summary>
/// Really only exists to make the intellisense/OM 'make sense' and to have a strongly types dictionary index
/// </summary>
/// <remarks></remarks>
public class clsPriceBand
{


    //IMPORTANT
    //In iQuote 1, resellers typically had accounts with Distributors - and pricing was heavily based on the 'hostAccountNum' eg.. CHA097
    //Some distis (Servers Plus) used Price Bands 'A' 'B' 'C' - mapping customers to one of a set of prices for a product
    //others (Ingram Europe) used 'Matrix' Pricing (essentially the same thing, but per product) - A Priceband with a compound key containinng the productId or Buyer probably addressed this (Next step)
    //If you think about - these *are the same thing*
    public string text;

    public clsPriceBand(string text)
    {

        //This constructor is ONLY called from iq.GetPriceband

        this.text = text;
        iq.priceBands.Add(text, this);

    }


}