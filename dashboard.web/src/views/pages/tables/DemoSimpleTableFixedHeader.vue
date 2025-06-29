<script setup lang="ts">

defineProps<{
  rows: Array<Array<string | number>>;
  headers: Array<string | number>;

  actions?: Array<{
    icon: string;
    label: string;
    onClick: (row: Array<string | number>) => void;
  }>;

}>();

</script>

<template>
  <VTable
    height="250"
    fixed-header
  >
    <thead>
      <tr>
        <th v-for="header in headers" :key="header as string | number">
          {{ header }}
        </th>
      </tr>
    </thead>

    <tbody>
      <tr
        v-for="row in rows"
        :key="rows.indexOf(row)"
      >
        <td
          v-for="cell in row"
          :key="row.indexOf(cell)"
        >
          {{ cell }}
        </td>
        <td v-if="actions">
          <VBtn
            v-for="action in actions"
            :key="action.label"
            icon
            @click="action.onClick(row)"
          >
            <VIcon :icon="action.icon" />
          </VBtn>
        </td>
      </tr>
    </tbody>
  </VTable>
</template>
      
