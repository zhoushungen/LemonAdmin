<template><div class="header"><div><h1 class="page-title">{{title}}</h1><div v-if="description" class="page-desc">{{description}}</div></div><div><slot/></div></div></template>
<script setup lang="ts">defineProps<{title:string;description?:string}>()</script>
<style scoped>.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;margin-bottom:16px}</style>
