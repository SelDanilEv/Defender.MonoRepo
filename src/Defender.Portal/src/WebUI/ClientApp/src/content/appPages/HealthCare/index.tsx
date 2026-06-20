import { useEffect, useMemo, useState } from "react";
import { Box, Button, Card, CardContent, Chip, Grid, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { healthCareApi, HealthEvent, HealthEventType } from "src/api/healthCare";

const nowInput = () => new Date().toISOString().slice(0, 16);

const HealthCarePage = () => {
  const [events, setEvents] = useState<HealthEvent[]>([]);
  const [type, setType] = useState<HealthEventType>("Temperature");
  const [startedAt, setStartedAt] = useState(nowInput());
  const [endedAt, setEndedAt] = useState(nowInput());
  const [temperature, setTemperature] = useState("37.0");
  const [medicationName, setMedicationName] = useState("");
  const [medicationAmount, setMedicationAmount] = useState("1");
  const [medicationUnit, setMedicationUnit] = useState("табл.");
  const [notes, setNotes] = useState("");

  const refresh = () => healthCareApi.getEvents().then(setEvents);

  useEffect(() => { refresh(); }, []);

  const chartEvents = useMemo(
    () => events.filter((event) => event.type === "Temperature" && event.temperatureCelsius !== undefined),
    [events]
  );

  const addEvent = async () => {
    await healthCareApi.createEvent({
      type,
      startedAt: new Date(startedAt).toISOString(),
      endedAt: type === "Sleep" ? new Date(endedAt).toISOString() : undefined,
      temperatureCelsius: type === "Temperature" ? Number(temperature) : undefined,
      medicationName: type === "Medication" ? medicationName : undefined,
      medicationAmount: type === "Medication" ? Number(medicationAmount) : undefined,
      medicationUnit: type === "Medication" ? medicationUnit : undefined,
      notes,
    });
    setNotes("");
    refresh();
  };

  const shareChart = async () => {
    const payload = btoa(unescape(encodeURIComponent(JSON.stringify(events))));
    const url = `${window.location.origin}/health-care/share?data=${payload}`;
    await navigator.clipboard.writeText(url);
  };

  const formatEvent = (event: HealthEvent) => {
    if (event.type === "Temperature") return `${event.temperatureCelsius} °C`;
    if (event.type === "Medication") return `${event.medicationName || "Лекарство"} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
    return `Сон до ${new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`;
  };

  return (
    <Box p={1}>
      <Stack direction="row" alignItems="center" gap={1} mb={2}>
        <LocalHospitalIcon color="primary" />
        <Typography variant="h3">Health Care</Typography>
        <Button variant="outlined" size="small" onClick={shareChart} disabled={events.length === 0}>Поделиться графиком</Button>
      </Stack>
      <Typography color="text.secondary" mb={2}>
        Отмечайте температуру, лекарства и сон в 30-минутном таймфрейме. График показывает только заполненные температурные фреймы, а сон и лекарства добавлены как события, чтобы не засорять пустыми интервалами.
      </Typography>

      <Grid container spacing={2}>
        <Grid item xs={12} md={4}>
          <Card><CardContent>
            <Stack gap={2}>
              <TextField select label="Тип события" value={type} onChange={(e) => setType(e.target.value as HealthEventType)} size="small">
                <MenuItem value="Temperature">Температура</MenuItem>
                <MenuItem value="Medication">Лекарство</MenuItem>
                <MenuItem value="Sleep">Сон</MenuItem>
              </TextField>
              <TextField label="Начало" type="datetime-local" value={startedAt} onChange={(e) => setStartedAt(e.target.value)} InputLabelProps={{ shrink: true }} size="small" />
              {type === "Sleep" && <TextField label="Конец сна" type="datetime-local" value={endedAt} onChange={(e) => setEndedAt(e.target.value)} InputLabelProps={{ shrink: true }} size="small" />}
              {type === "Temperature" && <TextField label="Температура, °C" value={temperature} onChange={(e) => setTemperature(e.target.value)} size="small" />}
              {type === "Medication" && <>
                <TextField label="Название" value={medicationName} onChange={(e) => setMedicationName(e.target.value)} size="small" />
                <TextField label="Количество" value={medicationAmount} onChange={(e) => setMedicationAmount(e.target.value)} size="small" />
                <TextField label="Единица" value={medicationUnit} onChange={(e) => setMedicationUnit(e.target.value)} size="small" />
              </>}
              <TextField label="Заметка" value={notes} onChange={(e) => setNotes(e.target.value)} size="small" multiline minRows={2} />
              <Button variant="contained" onClick={addEvent}>Добавить</Button>
            </Stack>
          </CardContent></Card>
        </Grid>

        <Grid item xs={12} md={8}>
          <Card><CardContent>
            <Typography variant="h4" mb={1}>График температуры</Typography>
            <LineChart
              height={300}
              xAxis={[{ scaleType: "point", data: chartEvents.map((event) => new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })) }]}
              series={[{ label: "°C", data: chartEvents.map((event) => event.temperatureCelsius || null) }]}
            />
            <Stack direction="row" flexWrap="wrap" gap={1} mt={2}>
              {events.map((event) => (
                <Chip key={event.id} label={`${new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })}: ${formatEvent(event)}`} onDelete={() => healthCareApi.deleteEvent(event.id).then(refresh)} color={event.type === "Sleep" ? "info" : event.type === "Medication" ? "secondary" : "primary"} variant="outlined" />
              ))}
            </Stack>
          </CardContent></Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default HealthCarePage;
