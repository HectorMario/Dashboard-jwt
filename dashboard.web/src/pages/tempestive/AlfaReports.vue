<script setup lang="ts">
    import DemoSimpleTableFixedHeader from '@/views/pages/tables/DemoSimpleTableFixedHeader.vue'

    const headers = ['Dessert (100g serving)', 'Calories', 'Fat (g)', 'Carbs (g)', 'Protein (g)']
    const rows = [
      ['Frozen yogurt', 159, 6.0, 24, 4.0],
      ['Ice cream sandwich', 237, 9.0, 37, 4.3],
      ['Eclair', 262, 16.0, 24, 6.0],
      ['Cupcake', 305, 3.7, 67, 4.3],
      ['Gingerbread', 356, 16.0, 49, 3.9]
    ]

    const actions = [
      {
        icon: 'ri-edit-line',
        label: 'Edit',
        onClick: (row: Array<string | number>) => {
          console.log('Edit action clicked for row:', row)
        }
      },
      {
        icon: 'ri-delete-bin-line',
        label: 'Delete',
        onClick: (row: Array<string | number>) => {
          console.log('Delete action clicked for row:', row)
        }
      }
    ]

</script>

<template>
  <VRow>  
    <VCol cols="12">
      <VCard title="Fixed Header">
        <VCardText>
          You can fix the header of table by using the <code>fixed-header</code> prop.
        </VCardText>
        <DemoSimpleTableFixedHeader 
          :headers="headers"
          :rows="rows"
          :actions="actions"
        />
      </VCard>
    </VCol>
  </VRow>
</template>