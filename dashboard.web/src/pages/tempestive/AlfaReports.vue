<script setup lang="ts">
    import { ref } from "vue"
    import { salvaAlfaReport } from "@/api/tempestive"

    const dialog = ref(false)
    const mese = ref(null)
    const anno = ref(null)
    const file = ref(null)

    const mesi = [
      { title: "Gennaio", value: 1 },
      { title: "Febbraio", value: 2 },
      { title: "Marzo", value: 3 },
      { title: "Aprile", value: 4 },
      { title: "Maggio", value: 5 },
      { title: "Giugno", value: 6 },
      { title: "Luglio", value: 7 },
      { title: "Agosto", value: 8 },
      { title: "Settembre", value: 9 },
      { title: "Ottobre", value: 10 },
      { title: "Novembre", value: 11 },
      { title: "Dicembre", value: 12 },
    ]

    // Anni dinamici (es. ultimi 10 anni)
    const currentYear = new Date().getFullYear()
    const anni = Array.from({ length: 10 }, (_, i) => currentYear - i)

    const salva = () => {
      if (!mese.value || !anno.value || !file.value) {
        alert("Compila tutti i campi e carica un file .xlsx")
        return
      }

      // Prepara il FormData per upload
      const formData = new FormData()
      formData.append("month", mese.value)
      formData.append("year", anno.value)
      formData.append("file", file.value)

      salvaAlfaReport(formData)
      .then((response) => {
        // Crea un URL temporaneo dal blob
       const blob = new Blob([response.data], { type: response.headers["content-type"] })
        const url = window.URL.createObjectURL(blob)

        let fileName = "report.xlsx"
        const cd = response.headers["content-disposition"]
        if (cd) {
          const match = cd.match(/filename="?([^"]+)"?/)
          if (match?.[1]) fileName = match[1]
        }

        const link = document.createElement("a")
        link.href = url
        link.setAttribute("download", fileName)
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        window.URL.revokeObjectURL(url)

        // Reset campi
        mese.value = null
        anno.value = null
        file.value = null
      })
      .catch((error) => {
        console.error("Errore durante il salvataggio del report:", error)
        alert("Si è verificato un errore durante il salvataggio del report.")
      })

      dialog.value = false
    }

</script>

<template>
  <VRow>
    <VCol cols="12">
      <VCard title="Alfa Reports" class="mb-4">
        <VCardText>
          <!-- Bottone per aprire il dialog -->
          <VBtn
            type="button"
            class="me-4"
            @click="dialog = true"
          >
            + New Report
          </VBtn>
        </VCardText>

        <DemoSimpleTableFixedHeader 
          
        />
      </VCard>
    </VCol>
  </VRow>

  <!-- MODALE -->
  <VDialog v-model="dialog" max-width="600">
    <VCard>
      <VCardTitle class="text-h6">Nuovo Report</VCardTitle>
      <VCardText>
        <VForm ref="form">
          <!-- Select Mese -->
          <VSelect
            v-model="mese"
            :items="mesi"
            label="Mese"
            required
          />

          <!-- Select Anno -->
          <VSelect
            v-model="anno"
            :items="anni"
            label="Anno"
            required
          />

          <!-- Upload file XLSX -->
          <VFileInput
            v-model="file"
            label="Carica file (.xlsx)"
            accept=".xlsx"
            show-size
            required
          />
        </VForm>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn color="secondary" @click="dialog = false">Chiudi</VBtn>
        <VBtn color="primary" class="me-4" @click="salva">Salva</VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>

