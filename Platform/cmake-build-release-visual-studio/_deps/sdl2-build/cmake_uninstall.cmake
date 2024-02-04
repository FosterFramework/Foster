if (NOT EXISTS "C:/Users/super/Downloads/dev_foster/Foster/Platform/cmake-build-release-visual-studio/install_manifest.txt")
    message(FATAL_ERROR "Cannot find install manifest: \"C:/Users/super/Downloads/dev_foster/Foster/Platform/cmake-build-release-visual-studio/install_manifest.txt\"")
endif(NOT EXISTS "C:/Users/super/Downloads/dev_foster/Foster/Platform/cmake-build-release-visual-studio/install_manifest.txt")

file(READ "C:/Users/super/Downloads/dev_foster/Foster/Platform/cmake-build-release-visual-studio/install_manifest.txt" files)
string(REGEX REPLACE "\n" ";" files "${files}")
foreach (file ${files})
    message(STATUS "Uninstalling \"$ENV{DESTDIR}${file}\"")
    execute_process(
        COMMAND C:/Users/super/AppData/Local/Programs/CLion/bin/cmake/win/x64/bin/cmake.exe -E remove "$ENV{DESTDIR}${file}"
        OUTPUT_VARIABLE rm_out
        RESULT_VARIABLE rm_retval
    )
    if(NOT ${rm_retval} EQUAL 0)
        message(FATAL_ERROR "Problem when removing \"$ENV{DESTDIR}${file}\"")
    endif (NOT ${rm_retval} EQUAL 0)
endforeach(file)

