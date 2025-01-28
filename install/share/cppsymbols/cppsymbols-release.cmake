#----------------------------------------------------------------
# Generated CMake target import file for configuration "Release".
#----------------------------------------------------------------

# Commands may need to know the format version.
set(CMAKE_IMPORT_FILE_VERSION 1)

# Import target "cppsym::cppsymbols" for configuration "Release"
set_property(TARGET cppsym::cppsymbols APPEND PROPERTY IMPORTED_CONFIGURATIONS RELEASE)
set_target_properties(cppsym::cppsymbols PROPERTIES
  IMPORTED_LOCATION_RELEASE "${_IMPORT_PREFIX}/bin/cppsymbols.exe"
  )

list(APPEND _cmake_import_check_targets cppsym::cppsymbols )
list(APPEND _cmake_import_check_files_for_cppsym::cppsymbols "${_IMPORT_PREFIX}/bin/cppsymbols.exe" )

# Commands beyond this point should not need to know the version.
set(CMAKE_IMPORT_FILE_VERSION)
