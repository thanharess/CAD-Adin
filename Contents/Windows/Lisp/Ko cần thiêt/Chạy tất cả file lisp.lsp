;;;===========================================================
;;; LỆNH: LOADALLLISP
;;; Tác dụng : Load toàn bộ file .LSP trong thư mục được chọn và tất cả subfolder
;;; Phiên bản : Rút gọn, ổn định, tương thích AutoCAD 2014–2025
;;;===========================================================

(vl-load-com)

(defun getfolder (msg / sh folder)
  "Hiển thị hộp thoại chọn thư mục bằng Shell.Application"
  (setq sh (vla-getInterfaceObject (vlax-get-acad-object) "Shell.Application"))
  (setq folder (vlax-invoke-method sh 'BrowseForFolder 0 msg 0))
  (if folder
    (setq folder (vlax-get-property (vlax-get-property folder 'Self) 'Path))
  )
  (vlax-release-object sh)
  folder
)

(defun joinpath (folder filename)
  "Nối đường dẫn với xử lý dấu \\"
  (cond
    ((wcmatch folder "*\\") (strcat folder filename))
    ((wcmatch folder "*/")  (strcat folder filename))
    (T (strcat folder "\\" filename))
  )
)

(defun get-lsp-recursive (folder / files subdirs f s all)
  "Đệ quy tìm tất cả file .lsp trong folder và subfolder"
  (setq all '())
  ;; Lấy file .lsp trong thư mục hiện tại
  (foreach f (vl-directory-files folder "*.lsp" 1)
    (setq all (cons (joinpath folder f) all))
  )
  ;; Lặp qua các thư mục con
  (foreach s (vl-directory-files folder nil -1)
    (if (and (not (member s '("." "..")))
             (vl-file-directory-p (joinpath folder s))
        )
      (setq all (append all (get-lsp-recursive (joinpath folder s))))
    )
  )
  all
)

(defun c:LOADALLLISP (/ path files file count loaded)
  (prompt "\nLỆNH: LOADALLLISP - Load toàn bộ file .LSP trong thư mục và subfolder.")
  (setq loaded 0 count 0)

  ;; Chọn thư mục
  (setq path (getfolder "Chọn thư mục chứa các file LISP (*.lsp):"))
  (if (not path)
    (progn (alert "Không chọn thư mục nào.") (exit))
  )

  ;; Thêm dấu \\ nếu cần
  (if (not (wcmatch path "*\\")) (setq path (strcat path "\\")))

  ;; Lấy danh sách file .lsp
  (setq files (get-lsp-recursive path))

  (if (not files)
    (alert "Không tìm thấy file .LSP nào trong thư mục và subfolder.")
    (progn
      (setq count (length files))
      (princ (strcat "\nTìm thấy " (itoa count) " file LISP. Đang load..."))

      ;; Load từng file, tránh dừng khi lỗi
      (foreach file files
        (if (not (vl-catch-all-error-p (vl-catch-all-apply 'load (list file nil))))
          (progn
            (setq loaded (1+ loaded))
            (princ (strcat "\n→ Đã load: " file))
          )
          (princ (strcat "\n→ Lỗi load: " file))
        )
      )

      (princ (strcat
        "\nHoàn thành: Load thành công "
        (itoa loaded) "/" (itoa count) " file."
      ))
    )
  )
  (princ)
)

(princ "\nGõ LOADALLLISP để chạy lệnh.")
(princ)
