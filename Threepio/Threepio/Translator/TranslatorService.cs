using System;
using System.Linq;
using System.Text;
using Microsoft;
using System.Net;
using System.Configuration;

namespace Threepio.Translator
{
    public class TranslatorService
    {
        private TranslatorContainer translatorContainer;

        public void InitializeTranslator()
        {
            Console.OutputEncoding = Encoding.UTF8;

            translatorContainer = new TranslatorContainer(new Uri(ConfigurationManager.ConnectionStrings["TranslatorUri"].ToString()));
            translatorContainer.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["TranslatorAccountKey"], ConfigurationManager.AppSettings["TranslatorAccountKey"]);
        }

        public string Translate(string inputString)
        {
            Language targetLanguage = new Language();
            targetLanguage.Code = "en";

            var sourceLanguage = DetectSourceLanguage(translatorContainer, inputString);
            var result = TranslateString(translatorContainer, inputString, sourceLanguage, targetLanguage);

            return result.Text;
        }

        private static DetectedLanguage DetectSourceLanguage(TranslatorContainer tc, string inputString)
        {
            // calling Detect gives us a DataServiceQuery which we can use to call the  
            // service 
            var translateQuery = tc.Detect(inputString);

            // since this is a console application, we do not want to do an asynchronous  
            // call to the service. Otherwise, the program thread would likely terminate 
            // before the result came back, causing our app to appear broken. 
            var detectedLanguages = translateQuery.Execute().ToList();

            // since the result of the query is a list, there might be multiple 
            // detected languages. In practice, however, I have only seen one. 
            // Some input strings, 'hi' for example, are obviously valid in  
            // English but produce other results, suggesting that the service 
            // only returns the first result. 
            if (detectedLanguages.Count() > 1)
            {
                Console.WriteLine("Possible source languages:");

                foreach (var language in detectedLanguages)
                {
                    Console.WriteLine("\t" + language.Code);
                }

                Console.WriteLine();
            }

            // only continue if the Microsoft Translator identified the source language 
            // if there are multiple, let's go with the first. 
            if (detectedLanguages.Count() > 0)
            {
                return detectedLanguages.First();
            }

            else
            {
                return null;
            }
        }

        private static Translation TranslateString(TranslatorContainer tc, string inputString, DetectedLanguage sourceLanguage, Language targetLanguage)
        {
            // Generate the query
            var translationQuery = tc.Translate(inputString, targetLanguage.Code, sourceLanguage.Code);

            // Call the query and get the results as a List
            var translationResults = translationQuery.Execute().ToList();

            // Verify there was a result
            if (translationResults.Count() <= 0)
            {
                return null;
            }

            // In case there were multiple results, pick the first one
            var translationResult = translationResults.First();

            return translationResult;
        }
    }
}
