<script setup>
import charlieTangoLogo from '../images/charlietango.svg'
</script>

<template>
  <img
    class="charlie-tango-logo"
    :src="charlieTangoLogo"
    alt="Charlie Tango"
  >
</template>

<style>
.charlie-tango-logo {
  position: absolute;
  top: 1.5rem;
  right: 2rem;
  z-index: 20;
  width: 9rem;
  height: auto;
  pointer-events: none;
}
</style>
