 

#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include "DHT.h"
#include <string>
#include <cstring>

#define DHTPIN 2
#define DHTTYPE DHT11   // DHT 11

const char* ssid = "iwifi";
const char* password = "12345678";
const char* mqtt_server = "m12.cloudmqtt.com";

WiFiClient espClient;
PubSubClient client(espClient);
long lastMsg = 0;
char msg[50];
int value = 0;

DHT dht(DHTPIN, DHTTYPE);

void setup() {
  pinMode(D1, OUTPUT);     // Initialize the BUILTIN_LED pin as an output
  Serial.begin(115200);

  dht.begin();
  
  setup_wifi();
  client.setServer(mqtt_server, 19518);
  client.setCallback(callback);
}

void setup_wifi() {

  delay(10);
  // We start by connecting to a WiFi network
  Serial.println();
  Serial.print("Connecting to ");
  Serial.println(ssid);

  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("");
  Serial.println("WiFi connected");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
}

void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.print("] ");
  for (int i = 0; i < length; i++) {
    Serial.print((char)payload[i]);
  }
  Serial.println();

  // Switch on the LED if an 1 was received as first character
  if ((char)payload[0] == '1') {
    digitalWrite(BUILTIN_LED, HIGH);   // Turn the LED on (Note that LOW is the voltage level
    // but actually the LED is on; this is because
    // it is acive low on the ESP-01)
  } else {
    digitalWrite(BUILTIN_LED, LOW);  // Turn the LED off by making the voltage HIGH
  }
}

void reconnect() {
  // Loop until we're reconnected
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
     
    if (client.connect("pwerrxxxxpopusjfcppsofzzzmu", "quwubvsq", "hbhaOkoKMFtF")) {
      Serial.println("connected"); 
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5 seconds"); 
      delay(5000);
    }
  }
}
void loop() {

  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  float h = dht.readHumidity(); 
  float t = dht.readTemperature(); 
  float f = dht.readTemperature(true);

  if (isnan(h) || isnan(t) || isnan(f)) {
    Serial.println("Failed to read from DHT sensor!");
    return;
  }
  float hif = dht.computeHeatIndex(f, h); 
  float hic = dht.computeHeatIndex(t, h, false);

  //Serial.print("Humidity: ");
  //Serial.print(h);
  //Serial.print(" %\t");
  //Serial.print("Temperature: ");
  //Serial.print(t);
  //Serial.print(" *C ");
  //Serial.print(f);
  //Serial.print(" *F\t");
  //Serial.print("Heat index: ");
  //Serial.print(hic);
  //Serial.print(" *C ");
  //Serial.print(hif);
  //Serial.println(" *F");

  //char msg1[100]; 

  String loc1 = "\"16.8972709,101.7576987\"}";
  String loc2 = "\"16.902479,101.740896\"}";
  String loc3 = "\"16.8955184,101.7336213\"}";
  
  String msg1 = "{\"ID\": \"tpr_001\",  \"H\": "+ String(h) +", \"T\": " + String(t) + ", \"Status\": -1, \"Location\": " + loc1;
  Serial.println("public => " + msg1);
  client.publish("teparak", msg1.c_str());
  delay(5000);

  //long now = millis();
  //if (now - lastMsg > 2000) {
  //  lastMsg = now;
  //  ++value;
  //  snprintf (msg, 75, "hello world #%ld", value);
  //  Serial.print("Publish message: ");
  //  Serial.println(msg);
  //  client.publish("teparak", msg);
  //}
}
