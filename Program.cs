using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;


namespace FidelizadorApiClient
{
    internal class JsonResponse
    {
        public int list_id { get; set; }
        public bool status { get; set; }
        public bool error { get; set; }
        public Dictionary<string,int> msg { get; set; }
    }

    class MainClass
    {
        public static void Main(string[] args)
        {
            string slug = "release";
            string client_id = "1_36imbsbmujmsocc44w4ks44w0cwk40gkkws0wgoggw84sgco08";
            string client_secret = "56tshkyljzk88cg4wgo0ckwswg80c0wsowwgossw8kskkkc0w0";
            string domain = "api.fidelizador.com";
            bool https = true;

            int campaign_id;
            string response;
            string path;
            NameValueCollection parameters;
            JsonResponse Entity = new JsonResponse();

            FidelizadorApiClient.Api Api = new FidelizadorApiClient.Api(domain, https);
            Api.Autenticate(slug, client_id, client_secret);
            Console.WriteLine("Autenticado");

            //Crear una lista
            path = "/1.0/list.json"; //puede usar también .xml en vez de .json
            parameters = new NameValueCollection
            {
                { "name", "Nombre de la nueva lista"}, //requerido
                { "fields[0]", "FIRSTNAME"}, //campo de fidelizador
                { "fields[1]", "LASTNAME"}, //campo de fidelizador
                { "fields[2]", "TELEFONO"}, //campo personalizado
            };
            /*
             * La definición de campos es opcional, pero de definirlos debe 
             * enumerarlos de 0 a n-1, el número solo es el orden pero no tiene
             * ningun aspecto funcional.
             */
            response = Api.Request("POST", path, parameters);
            Entity = JsonConvert.DeserializeObject<JsonResponse>(response);
            Console.WriteLine(response);

            //Crear una campaña
            string html = @"
            <html>
                <head>
                    <title>Campaña de newsletter</title>
                </head>
                <body>
                    <p>Contenido del newsletter</p>
                    <p>
                        <a href=""#"" class=""fide-remove-subscriber"">Desuscríbase</a> | 
                        <a href=""#"" class=""fide-abuse"">Reportar abuso</a>
                    </p>
            	</body>
            </html>";
            path = "/1.0/campaign.json"; //Puede usar también .xml en vez de .json
            parameters = new NameValueCollection
            {
                { "name", "Nombre de la nueva campaña"}, //Requerido.
                { "type", "1"}, //Campaña de Newletter.
                { "list_id", Entity.list_id.ToString()}, //ID de lista existente.
                { "category_id", "1"}, //ID de categoria existente, el ID de a categoría por defecto de fidelizador es 1.
                { "subject", "Asunto del correo electrónico"}, //Requerido.
                { "to_name", "fidelizador"}, //Nombre de la persona a quien responder el correo.
                { "reply_to", "micorreo@midominio.cl"}, //Correo de la presona a quien responder el correo.
                { "from_email", "remitente.valido@midominio.cl" }, //Remitente que enviará el correo. Este debe estar registrado previamente en fidelizador.
                { "content", html }, //Diseño del neswletter (opcional).
            };
            try
            {
                response = Api.Request("POST", path, parameters);
                Entity = JsonConvert.DeserializeObject<JsonResponse>(response);
                campaign_id = Entity.msg["campaign_id"];
                Console.WriteLine(response);
            }
            catch (WebException e)
            {
                Console.WriteLine(string.Format("No se puede crear la campaña: {0}", e.Message));
                campaign_id = 0;
            }

            /*
             * Advertencia: Para programar una campaña debe cumplir con estos requisitos:
             *  - El HTML de la campaña contiene el enlace de desuscripción con alguno de estos textos:
             *    "Desuscripción", "Desuscribirse", "Desuscribir", "Desuscríbete", "Desuscríbase"
             *  - Debe incluir en el html el enlace para reportar abuso.
             *  - La lista seleccionada tiene almenos 1 contacto para depachar.
             *  - Si la categoría de la campaña usa politicas de contactabilidad, al menos debe 
             *    tener 1 contato para depachar.
             */

            //Primer método: programar una campaña para una fecha futura.
            path = string.Format("/1.0/campaign/{0}/schedule.json", campaign_id.ToString());
            parameters = new NameValueCollection
            {
                { "scheduled_at", "2020-12-25 00:00:00"},
            };
            Console.WriteLine(string.Format("Programando campaña {0} para el {1} ...", campaign_id, parameters["scheduled_at"]));
            try
            {
                response = Api.Request("POST", path, parameters);
                Console.WriteLine(response);
            }
            catch (WebException e) {
                Console.WriteLine(string.Format("No se puede programar esta campaña: {0}", e.Message));
            }

            //Segundo método: programar una campaña para envío inmediato.
            path = string.Format("/1.0/campaign/{0}/schedule.json", campaign_id.ToString());
            parameters = new NameValueCollection
            {
                { "send_now", "true"},
            };
            Console.WriteLine(string.Format("Programando campaña {0} ahora ...", campaign_id));
            try
            {
                response = Api.Request("POST", path, parameters);
                Console.WriteLine(response);
            }
            catch (WebException e)
            {
                Console.WriteLine(string.Format("No se puede programar esta campaña: {0}", e.Message));
            }
        }
    }
}
