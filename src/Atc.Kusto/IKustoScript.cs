namespace Atc.Kusto;

public interface IKustoScript
{
    /// <summary>
    /// Retrieves the Kusto query text from an embedded resource based on the
    /// full name of the derived class. The query text is expected to be embedded
    /// as a `.kusto` file resource within the assembly.
    /// </summary>
    /// <returns>The Kusto query text as a string.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the embedded resource could not be found.</exception>
    string GetQueryText();

    /// <summary>
    /// Retrieves a dictionary of parameters for the Kusto script by extracting
    /// the public instance properties of the derived class and converting their names to camelCase.
    /// This allows the script to pass parameters dynamically based on the object's state.
    /// </summary>
    /// <returns>A dictionary containing the script parameters as key-value pairs.</returns>
    IDictionary<string, object> GetParameters();
}