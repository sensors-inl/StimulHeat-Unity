using System;
using System.Collections.Generic;
using Google.Protobuf;
using Stimulheat.Bluetooth;

[Serializable]
public class StimulHeatDevice
{
    public string deviceName;
    public string deviceId;
    public bool connected;
    public byte[] enslavementProtocolToSend;
    public byte[] commandProtocolToSend;
    public BLE.BLEData receivedProtocol;
    public MeasurementsResults measurementsResults;
    public float temperature;

    public override string ToString()
    {
        return "StimulHeat Device -> Name: " + deviceName + " ID: " + deviceId + " Connected: " + connected;
    }

    public void UpdateMeasurementResults()
    {
        var zeroIndex = Array.IndexOf(receivedProtocol.buf, byte.MinValue);
        var message = receivedProtocol.buf[0..zeroIndex];
        var decoded = COBS.NET.COBS.Decode(message);
        try
        {
            var protocol = Protocol.Parser.ParseFrom(decoded);
            measurementsResults = protocol.Stream.MeasurementsResults;
            temperature = measurementsResults.TempIn / 100f;
        }
        catch { }
    }

    public void UpdateCommandEnslavement(EnslavementType type)
    {
        var enslavementProtocol = new Protocol()
        {
            Request = new Request()
            {
                SetMeasureParametersList = new SetMeasureParametersList
                {
                    MeasureParametersList = new MeasureParametersList
                    {
                        EnslavementType = type,
                        PidParameters = new PIDParameters()
                        {
                            Kp = 1000,
                            Ki = 0,
                            Kd = 0,
                            Dt = 0.1f
                        },
                        TedParameters = new TEDParameters()
                        {
                            Am = 0.029f,
                            Rm = 3f,
                            Tm = 10.17f
                        }
                    }
                }
            }
        };

        enslavementProtocolToSend = enslavementProtocol.ToByteArray();
        enslavementProtocolToSend = COBS.NET.COBS.Encode(enslavementProtocolToSend);
    }

    public void UpdateCommandValue(float value, bool enabled)
    {
        var valueProtocol = new Protocol
        {
            Request = new Request
            {
                Command = new Command
                {
                    CommandValue = value,
                    Enabled = enabled
                }
            }
        };

        commandProtocolToSend = valueProtocol.ToByteArray();
        commandProtocolToSend = COBS.NET.COBS.Encode(commandProtocolToSend);
    }

    public void UpdateCommandIntensity(CommandIntensity intensity, bool enabled)
    {
        var intensityProtocol = new Protocol
        {
            Request = new Request
            {
                Command = new Command
                {
                    CommandIntensity = intensity,
                    Enabled = enabled
                }
            }
        };
        commandProtocolToSend = intensityProtocol.ToByteArray();
        commandProtocolToSend = COBS.NET.COBS.Encode(commandProtocolToSend);
    }
}

