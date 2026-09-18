
;; lệnh coppy dim style đã có sẵn, có 2 lựa chọn: 1 là scale dim to, 2 scale dim nhỏ bằng phương pháp new style theo gốc được lựa chọn trong bản vẽ


(defun c:DIMSCALESYLENEW ( / *error* ent dim-obj oldStyle scaleVal newStyleName choice mode acadApp doc dimStyles oldStyleObj newStyle entData)
  ;; Error handler đơn giản
  (defun *error* (msg)
    (if (and acadApp (vlax-object-p acadApp)) (vlax-release-object acadApp))
    (if (and doc (vlax-object-p doc)) (vlax-release-object doc))
    (if (and dimStyles (vlax-object-p dimStyles)) (vlax-release-object dimStyles))
    (if (and oldStyleObj (vlax-object-p oldStyleObj)) (vlax-release-object oldStyleObj))
    (if (and newStyle (vlax-object-p newStyle)) (vlax-release-object newStyle))
    (if msg (princ (strcat "\nLỗi: " msg)))
    (princ)
  )

  (vl-load-com)
  (setq acadApp (vlax-get-acad-object))
  (setq doc (vla-get-ActiveDocument acadApp))
  (setq dimStyles (vla-get-DimStyles doc))

  ;; Bước 1: Chọn DIMENSION entity để lấy style gốc
  (princ "\nChọn một DIMENSION để lấy Dimstyle gốc: ")
  (if (not (setq ent (car (entsel))))
    (progn (princ "\nKhông chọn được DIMENSION. Hủy lệnh.") (exit))
  )
  (if (not (= (cdr (assoc 0 (entget ent))) "DIMENSION"))
    (progn (princ "\nĐối tượng chọn không phải DIMENSION. Hủy lệnh.") (exit))
  )
  (setq dim-obj (vlax-ename->vla-object ent))
  (setq oldStyle (vla-get-StyleName dim-obj))
(if (vl-string-search "$" oldStyle)
  (setq oldStyle (substr oldStyle 1 (vl-string-search "$" oldStyle)))
)

  (princ (strcat "\nDimstyle gốc: " oldStyle))

  ;; Bước 2: Chọn chế độ
  (initget "1 2")
  (setq choice (getkword "\nChọn chế độ [1:Phóng to / 2:Thu nhỏ] <1>: "))
  (if (not choice) (setq choice "1"))
  (cond
    ((= choice "1") (setq mode "Phóng to"))
    ((= choice "2") (setq mode "Thu nhỏ"))
    (t (setq mode "Phóng to" choice "1"))
  )
  (princ (strcat "\nChế độ: " mode))

  ;; Bước 3: Nhập hệ số scale
  (while (or (not scaleVal) (<= scaleVal 0))
    (setq scaleVal (getreal "\nNhập hệ số scale >0 (VD: 2 = gấp đôi kích thước): "))
    (if (not scaleVal) (setq scaleVal 1.0))
  )
  (if (= choice "2") (setq scaleVal (/ 1.0 scaleVal)))
  (princ (strcat "\nHệ số scale áp dụng: " (rtos scaleVal 2 2)))

  ;; Bước 4: Tạo tên mới
  (setq newStyleName
  (if (= choice "2")  ; Nếu là chế độ thu nhỏ
    (strcat oldStyle " Scale 1 chia " (rtos (/ 1 scaleVal) 2 2))  ; Ví dụ: "S30 Scale 1 chia 3"
    (strcat oldStyle " Scale " (rtos scaleVal 2 2))               ; Ví dụ: "S30 Scale 3"
  )
)

  (if (> (strlen newStyleName) 31)
    (setq newStyleName (substr newStyleName 1 31))
  )
  (princ (strcat "\nTên style mới: " newStyleName))

  ;; Bước 5: Kiểm tra tồn tại
  (if (tblsearch "DIMSTYLE" newStyleName)
    (progn
      (initget "Yes No")
      (if (/= (getkword (strcat "\nDimstyle '" newStyleName "' đã tồn tại. Ghi đè? [Yes/No] <Yes>: ")) "No")
        (progn
          (princ "\n⚠️ Đang xóa và tạo mới...")
          (vl-catch-all-apply 'vla-Delete (list (vla-Item dimStyles newStyleName)))
        )
        (progn (princ "\n⏹️ Hủy lệnh.") (*error* nil) (exit))
      )
    )
  )

  ;; Bước 6: Tạo style mới
  (setq oldStyleObj (vla-Item dimStyles oldStyle))
  (vl-catch-all-apply 'vla-Add (list dimStyles newStyleName))
  (setq newStyle (vla-Item dimStyles newStyleName))
  (if (not (vl-catch-all-error-p (vl-catch-all-apply 'vla-CopyFrom (list newStyle oldStyleObj))))
    (progn
      ;; Ghi DIMLFAC bằng entmod
      (setq ent (tblobjname "DIMSTYLE" newStyleName))
      (setq entData (entget ent))
      (if (assoc 144 entData)
        (setq entData (subst (cons 144 scaleVal) (assoc 144 entData) entData))
        (setq entData (append entData (list (cons 144 scaleVal))))
      )
      (entmod entData)
      (entupd ent)

      ;; Đặt làm current
      (setvar "DIMSTYLE" newStyleName)

      (princ (strcat
        "\n✅ Đã tạo Dimstyle mới giống Dimension Style Manager:"
        "\n   - Tên: " newStyleName
        "\n   - Sao chép từ: " oldStyle
        "\n   - Measurement Scale Factor: " (rtos scaleVal 2 2)
        "\n(Đã đặt làm Current. Mở DIMSTYLE để kiểm tra Preview.)"
      ))
    )
    (progn
      (alert (strcat "Lỗi tạo/sao chép style '" newStyleName "'! Kiểm tra quyền hoặc tên hợp lệ."))
      (*error* nil)
      (exit)
    )
  )
  (*error* nil)
  (princ)
)

(princ "\n:: Lệnh DIMSCALESYLENEW đã tải (Phiên bản sửa: dùng entmod để ghi DIMLFAC).")
(princ "\n:: Gõ DIMSCALESYLENEW để chạy.")
(princ)
